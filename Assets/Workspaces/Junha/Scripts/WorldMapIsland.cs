using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 월드맵의 섬 하나: 상태에 따라 섬과 라벨 색을 바꾸고, 라벨에 Act 제목을 표시한다.
// 현재 Act 섬은 위아래로 떠다니고, 마우스를 올리면 커지며, 누르면 출발한다.
// 잠긴 섬은 누르면 흔들리기만 한다. (라벨은 섬의 자식이라 라벨을 눌러도 동작)
// 그림자는 섬의 자식이 아니라 지도 위에 따로 두어, 섬이 뜨면 작고 연해지고 내려오면 커지고 진해진다.
// 아직 열리지 않은 섬은 안개에 덮여 있고(라벨은 안개 위), 열린 섬은 가운데 띠 안개만 옅게 남는다.
// 새로 열리는 섬은 양옆 덩어리 안개가 밀려나며 사라지고 띠 안개가 옅어지는 연출을 한다.
// 클리어한 섬에는 클리어 도장이 찍혀 있고, 방금 클리어한 섬은 도장이 쾅 찍히는 연출을 한다.
[RequireComponent(typeof(Image))]
public class WorldMapIsland : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("라벨")]
    [SerializeField] private Image labelImage;
    [SerializeField] private TMP_Text actNumberText;
    [SerializeField] private TMP_Text titleText;

    [Header("클리어 도장 (섬의 마지막 자식이라 라벨 위에 그려짐)")]
    [SerializeField] private Image clearStamp;
    [Tooltip("도장이 내려찍히는 시간")]
    [SerializeField] private float stampDropTime = 0.25f;
    [Tooltip("도장이 내려오기 시작할 때의 크기 배율")]
    [SerializeField] private float stampStartScale = 2.5f;
    [Tooltip("도장이 내려오기 시작할 때 더 기울어져 있는 각도")]
    [SerializeField] private float stampStartTilt = 15f;
    [Tooltip("찍힌 뒤 살짝 눌렸다가 돌아오는 시간")]
    [SerializeField] private float stampSettleTime = 0.15f;
    [Tooltip("도장이 닿기 몇 초 전에 효과음을 낼지 (효과음 앞부분에 여백이 있으면 늘리기, 도장이 나타나기 전부터도 가능)")]
    [SerializeField] private float stampSoundLead = 0.5f;

    [Header("그림자 (섬 뒤에 따로 배치)")]
    [SerializeField] private Image shadow;
    [Tooltip("섬이 가장 높이 떴을 때 그림자가 줄어드는 비율")]
    [SerializeField] private float shadowFloatScale = 0.08f;
    [Tooltip("섬이 가장 높이 떴을 때 그림자가 연해지는 비율")]
    [SerializeField] private float shadowFloatAlpha = 0.3f;
    [Tooltip("잠긴 섬의 그림자 진하기 배율")]
    [SerializeField] private float lockedShadowAlpha = 0.5f;

    [Header("안개 (섬의 첫 번째 자식이라 라벨보다 아래에 그려짐)")]
    [SerializeField] private GameObject fogRoot;
    [SerializeField] private Image fogDense;
    [SerializeField] private Image fogMidA;
    [SerializeField] private Image fogMidB;
    [SerializeField] private float fogDriftDistance = 25f;
    [SerializeField] private float fogDriftSpeed = 0.35f;
    [SerializeField] private float fogRevealTime = 1.6f;
    [Tooltip("걷힐 때 양옆 안개가 밀려나는 거리 (섬 너비 대비)")]
    [SerializeField] private float fogSidePush = 0.38f;
    [Tooltip("걷힌 뒤 가운데 띠 안개(fogDense)가 남는 진하기 배율")]
    [SerializeField] private float fogBandRemainAlpha = 0.5f;

    [Header("연출")]
    [SerializeField] private float floatAmplitude = 10f;
    [SerializeField] private float floatSpeed = 1.5f;
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float scaleLerpSpeed = 10f;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeStrength = 12f;

    private WorldMapManager manager;
    private RectTransform rect;
    private Image image;
    private int act;
    private WorldMapManager.IslandState state;
    private Vector2 basePosition;
    private bool hovered;
    private float shakeTimer;
    private bool entering;
    private CanvasGroup canvasGroup;

    private RectTransform shadowRect;
    private Color shadowBaseColor;
    private Vector3 shadowBaseScale;
    private float shadowStateAlpha = 1f;
    private float groupAlpha = 1f;
    private float lift = -1f; // -1: 가장 낮음 ~ 1: 가장 높음

    private bool fogRevealing;
    private float fogReveal;   // 0: 덮임 ~ 1: 걷힘
    private Vector2 fogMidABase;
    private Vector2 fogMidBBase;
    private Vector3 fogDenseScale;
    private Vector2 fogDenseBase;
    private float fogDenseAlpha;
    private float fogMidAAlpha;
    private float fogMidBAlpha;
    private float fogDriftOffset;

    private Vector3 stampBaseScale;
    private Quaternion stampBaseRotation;

    // startFogged: 잠긴 섬이거나, 이번에 안개가 걷히는 연출을 할 섬이면 true
    // hideStamp: 이번에 도장이 찍히는 연출을 할 섬이면 true (연출 전까지 도장을 숨김)
    public void Init(WorldMapManager owner, int actNumber, string title, WorldMapManager.IslandState islandState,
        Color clearedColor, Color lockedColor, bool startFogged, bool hideStamp)
    {
        manager = owner;
        act = actNumber;
        state = islandState;

        rect = (RectTransform)transform;
        image = GetComponent<Image>();
        basePosition = rect.anchoredPosition;

        // 진입 연출 때 섬과 라벨을 한꺼번에 흐리게 하기 위함
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 투명한 여백은 클릭되지 않도록 (텍스처 Read/Write가 꺼져 있으면 사각형 전체가 클릭됨)
        if (image.sprite != null && image.sprite.texture.isReadable)
            image.alphaHitTestMinimumThreshold = 0.1f;

        Color tint = state switch
        {
            WorldMapManager.IslandState.Cleared => clearedColor,
            WorldMapManager.IslandState.Locked => lockedColor,
            _ => Color.white,
        };
        image.color = tint;

        // 라벨은 글씨가 읽히도록 섬보다 절반만 어둡게
        if (labelImage != null)
            labelImage.color = Color.Lerp(Color.white, tint, 0.5f);

        if (actNumberText != null)
        {
            // 도장이 없을 때만 라벨에 클리어 표시
            actNumberText.text = state == WorldMapManager.IslandState.Cleared && clearStamp == null
                ? $"ACT {act} · 클리어"
                : $"ACT {act}";
        }

        if (titleText != null)
            titleText.text = title;

        if (clearStamp != null)
        {
            RectTransform stampRect = clearStamp.rectTransform;
            stampBaseScale = stampRect.localScale;
            stampBaseRotation = stampRect.localRotation;
            clearStamp.raycastTarget = false;
            clearStamp.gameObject.SetActive(state == WorldMapManager.IslandState.Cleared);
            if (hideStamp)
                SetGraphicAlpha(clearStamp, 0f);
        }

        if (shadow != null)
        {
            shadowRect = shadow.rectTransform;
            shadowBaseColor = shadow.color;
            shadowBaseScale = shadowRect.localScale;
            shadow.raycastTarget = false;
            shadowStateAlpha = state == WorldMapManager.IslandState.Locked ? lockedShadowAlpha : 1f;
            ApplyShadow();
        }

        if (fogDense != null)
        {
            fogDenseScale = fogDense.rectTransform.localScale;
            fogDenseBase = fogDense.rectTransform.anchoredPosition;
            fogDenseAlpha = fogDense.color.a;
        }
        if (fogMidA != null)
        {
            fogMidABase = fogMidA.rectTransform.anchoredPosition;
            fogMidAAlpha = fogMidA.color.a;
        }
        if (fogMidB != null)
        {
            fogMidBBase = fogMidB.rectTransform.anchoredPosition;
            fogMidBAlpha = fogMidB.color.a;
        }
        // 섬마다 안개가 흐르는 박자가 다르게
        fogDriftOffset = act * 1.7f;
        fogReveal = startFogged ? 0f : 1f;
        ApplyFog();
    }

    // delay초 기다린 뒤 도장이 크게 들린 채 나타나 빠르게 내려찍히고(효과음 + 섬 흔들림), 살짝 눌렸다가 제자리로
    // 효과음은 닿는 순간보다 stampSoundLead초 먼저 (기다리는 중에도 날 수 있음)
    public IEnumerator PlayStamp(float delay)
    {
        if (clearStamp == null || !clearStamp.gameObject.activeSelf) yield break;

        RectTransform stampRect = clearStamp.rectTransform;
        Quaternion startRotation = stampBaseRotation * Quaternion.Euler(0f, 0f, stampStartTilt);

        float soundTime = Mathf.Max(0f, delay + stampDropTime - stampSoundLead);
        bool soundPlayed = false;
        float elapsed = 0f;

        while (elapsed < delay)
        {
            TryPlayStampSound(elapsed, soundTime, ref soundPlayed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        float time = 0f;
        while (time < stampDropTime)
        {
            TryPlayStampSound(delay + time, soundTime, ref soundPlayed);
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / stampDropTime);
            float e = t * t; // 갈수록 빠르게 (ease-in)
            stampRect.localScale = stampBaseScale * Mathf.Lerp(stampStartScale, 1f, e);
            stampRect.localRotation = Quaternion.Slerp(startRotation, stampBaseRotation, e);
            SetGraphicAlpha(clearStamp, Mathf.Clamp01(t * 2.5f));
            yield return null;
        }

        stampRect.localScale = stampBaseScale;
        stampRect.localRotation = stampBaseRotation;
        SetGraphicAlpha(clearStamp, 1f);

        if (!soundPlayed && manager != null)
            manager.PlayStampSound();
        shakeTimer = shakeDuration;

        time = 0f;
        while (time < stampSettleTime)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / stampSettleTime);
            stampRect.localScale = stampBaseScale * (1f - 0.06f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }

        stampRect.localScale = stampBaseScale;
    }

    private void TryPlayStampSound(float now, float soundTime, ref bool played)
    {
        if (played || now < soundTime) return;

        played = true;
        if (manager != null)
            manager.PlayStampSound();
    }

    // 덮인 안개가 걷힘: 양옆 덩어리는 가장자리로 밀려나며 사라지고, 가운데 띠 안개는 살짝 퍼지며 옅게 남음
    public IEnumerator RevealFog()
    {
        if (fogRoot == null || fogReveal >= 1f) yield break;

        fogRevealing = true;

        float time = 0f;
        while (time < fogRevealTime)
        {
            time += Time.deltaTime;
            fogReveal = Mathf.Clamp01(time / fogRevealTime);
            ApplyFog();
            yield return null;
        }

        fogReveal = 1f;
        fogRevealing = false;
        ApplyFog();
    }

    // fogReveal(0: 덮임 ~ 1: 걷힘)에 맞춰 안개 레이어 배치 + 천천히 좌우로 흐름
    private void ApplyFog()
    {
        if (fogRoot == null) return;

        float r = fogReveal;
        float easeOut = 1f - (1f - r) * (1f - r);
        float drift = Mathf.Sin((Time.time + fogDriftOffset) * fogDriftSpeed) * fogDriftDistance;
        float push = rect.rect.width * fogSidePush * easeOut;
        float midAlpha = 1f - r;

        if (fogDense != null)
        {
            fogDense.rectTransform.localScale = fogDenseScale * Mathf.Lerp(1f, 1.15f, easeOut);
            fogDense.rectTransform.anchoredPosition = fogDenseBase + new Vector2(drift * 0.5f, 0f);
            SetGraphicAlpha(fogDense, fogDenseAlpha * Mathf.Lerp(1f, fogBandRemainAlpha, easeOut));
        }
        if (fogMidA != null)
        {
            fogMidA.rectTransform.anchoredPosition = fogMidABase + new Vector2(-push + drift, 0f);
            SetGraphicAlpha(fogMidA, fogMidAAlpha * midAlpha);
            fogMidA.enabled = midAlpha > 0f;
        }
        if (fogMidB != null)
        {
            fogMidB.rectTransform.anchoredPosition = fogMidBBase + new Vector2(push - drift, 0f);
            SetGraphicAlpha(fogMidB, fogMidBAlpha * midAlpha);
            fogMidB.enabled = midAlpha > 0f;
        }
    }

    private static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    private void ApplyShadow()
    {
        if (shadow == null) return;

        float height = (lift + 1f) * 0.5f; // 0: 가장 낮음 ~ 1: 가장 높음
        shadowRect.localScale = shadowBaseScale * (1f - shadowFloatScale * height);

        Color c = shadowBaseColor;
        c.a *= shadowStateAlpha * groupAlpha * (1f - shadowFloatAlpha * height);
        shadow.color = c;
    }

    // 진입 연출 시작: 떠다니기·흔들기·호버를 멈추고 현재 모습 그대로 고정
    public void BeginEnter()
    {
        entering = true;
        hovered = false;
    }

    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        groupAlpha = alpha;
        ApplyShadow();
    }

    private void Update()
    {
        if (rect == null || entering) return;

        if (!fogRevealing)
            ApplyFog();

        Vector2 offset = Vector2.zero;

        if (state == WorldMapManager.IslandState.Current)
        {
            lift = Mathf.Sin(Time.time * floatSpeed);
            offset.y = lift * floatAmplitude;
            ApplyShadow();
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(shakeTimer / shakeDuration);
            offset.x = Mathf.Sin(Time.time * 60f) * shakeStrength * t;
        }

        rect.anchoredPosition = basePosition + offset;

        float target = hovered ? hoverScale : 1f;
        float scale = Mathf.Lerp(rect.localScale.x, target, Time.deltaTime * scaleLerpSpeed);
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (entering || state != WorldMapManager.IslandState.Current) return;

        hovered = true;
        if (manager != null)
            manager.PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager == null) return;

        switch (state)
        {
            case WorldMapManager.IslandState.Current:
                manager.Depart(act);
                break;
            case WorldMapManager.IslandState.Locked:
                shakeTimer = shakeDuration;
                manager.ShowLockedMessage(act);
                break;
        }
    }
}
