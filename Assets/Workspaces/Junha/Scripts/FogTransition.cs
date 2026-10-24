using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 안개로 화면을 덮은 뒤 씬을 바꾸고, 새 씬에서 안개가 양옆으로 걷히며 화면이 드러나는 전환.
// 씬이 바뀌어도 사라지지 않는 별도 오버레이 캔버스에 그리므로 도착 씬에는 아무것도 배치할 필요가 없다.
// 안개가 덮여 있는 동안 Act 제목을 보여주고, 안개가 걷힐 때 함께 사라진다.
// (월드맵 → 맵 진입에서 사용. 전투/휴식에서 맵으로 돌아올 때는 재생되지 않음)
public class FogTransition : MonoBehaviour
{
    // 다른 UI(플레이 타임 표시 1000 포함)보다 위에 그림
    private const int SortingOrder = 1100;
    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    // 안개 이미지가 화면 가장자리에서 끊겨 보이지 않도록 화면보다 크게
    private const float LayerOverscale = 1.25f;

    // 제목 폰트 (Assets/Workspaces/Junha/Resources 안의 폰트 에셋 이름)
    private const string TitleFontName = "온글잎 콘콘체 SDF";
    private static readonly Color ActNumberColor = new Color(0.95f, 0.85f, 0.6f);

    private Image back;
    private RectTransform dense;
    private RectTransform midLeft;
    private RectTransform midRight;
    private RectTransform thin;
    private Image denseImage;
    private Image midLeftImage;
    private Image midRightImage;
    private Image thinImage;

    private CanvasGroup titleGroup;
    private TextMeshProUGUI actNumberText;
    private TextMeshProUGUI titleText;

    private float holdTime;
    private float clearTime;
    private string targetScene;

    // 안개 레이어를 만든다 (처음엔 투명). dense: 짙은 안개, mid: 중간 덩어리, thin: 옅은 띠
    public static FogTransition Create(Sprite denseSprite, Sprite midSprite, Sprite thinSprite, Color backColor)
    {
        GameObject root = new GameObject("FogTransition");
        DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        // 전환 중 뒤쪽 UI 클릭 막기
        root.AddComponent<GraphicRaycaster>();

        FogTransition fog = root.AddComponent<FogTransition>();

        // 안개 사이 빈틈을 메우는 단색 배경 (클릭 차단 겸용)
        fog.back = CreateImage(root.transform, "Back", null, Vector2.zero);
        RectTransform backRect = fog.back.rectTransform;
        backRect.anchorMin = Vector2.zero;
        backRect.anchorMax = Vector2.one;
        backRect.sizeDelta = Vector2.zero;
        fog.back.color = new Color(backColor.r, backColor.g, backColor.b, 0f);
        fog.back.raycastTarget = true;

        Vector2 size = ReferenceResolution * LayerOverscale;
        fog.thinImage = CreateImage(root.transform, "Thin", thinSprite, size);
        fog.midLeftImage = CreateImage(root.transform, "MidLeft", midSprite, size);
        fog.midRightImage = CreateImage(root.transform, "MidRight", midSprite, size);
        fog.denseImage = CreateImage(root.transform, "Dense", denseSprite, size);

        fog.thin = fog.thinImage.rectTransform;
        fog.midLeft = fog.midLeftImage.rectTransform;
        fog.midRight = fog.midRightImage.rectTransform;
        fog.dense = fog.denseImage.rectTransform;

        // 오른쪽 덩어리는 좌우 반전해서 같은 그림이 반복돼 보이지 않게
        fog.midRight.localScale = new Vector3(-1f, 1f, 1f);

        fog.CreateTitle(root.transform);

        fog.SetCover(0f);
        return fog;
    }

    // 안개 위에 띄울 Act 제목 (예: "ACT 1", "잊혀진 숲의 유적")
    public void SetTitle(string actNumber, string title)
    {
        actNumberText.text = actNumber;
        titleText.text = title;
    }

    private void CreateTitle(Transform parent)
    {
        GameObject group = new GameObject("Title", typeof(RectTransform));
        group.transform.SetParent(parent, false);
        ((RectTransform)group.transform).sizeDelta = new Vector2(1400f, 300f);

        titleGroup = group.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        titleGroup.blocksRaycasts = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(TitleFontName);
        if (font == null)
            Debug.LogWarning($"[FogTransition] Resources에서 폰트를 찾을 수 없습니다: {TitleFontName}");

        actNumberText = CreateText(group.transform, "ActNumber", font, 48f, ActNumberColor, new Vector2(0f, 60f), 70f);
        titleText = CreateText(group.transform, "ActTitle", font, 84f, Color.white, new Vector2(0f, -30f), 110f);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, TMP_FontAsset font, float size,
        Color color, Vector2 position, float height)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(1400f, height);
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        image.enabled = sprite != null || name == "Back";
        return image;
    }

    // 안개가 몰려와 화면을 덮음
    public IEnumerator Cover(float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += StepTime();
            SetCover(EaseOut(Mathf.Clamp01(time / duration)));
            yield return null;
        }
        SetCover(1f);
    }

    // 화면이 덮인 상태에서 씬을 바꾸고, 새 씬에서 안개를 걷어낸 뒤 스스로 사라짐
    public void LoadSceneAndClear(string sceneName, float hold, float clear)
    {
        targetScene = sceneName;
        holdTime = hold;
        clearTime = clear;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive || scene.name != targetScene) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 덮는 연출이 덜 끝났어도 완전히 덮은 상태에서 걷기 시작
        StopAllCoroutines();
        SetCover(1f);
        StartCoroutine(ClearRoutine());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private IEnumerator ClearRoutine()
    {
        // 씬 로딩 직후 프레임이 튀는 동안은 덮인 채로 잠깐 유지
        float time = 0f;
        while (time < holdTime)
        {
            time += StepTime();
            yield return null;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fogClearSound);

        time = 0f;
        while (time < clearTime)
        {
            time += StepTime();
            SetClear(Mathf.Clamp01(time / clearTime));
            yield return null;
        }

        Destroy(gameObject);
    }

    // 덮기: 0(없음) ~ 1(완전히 덮음)
    private void SetCover(float t)
    {
        SetAlpha(back, Mathf.Clamp01((t - 0.4f) / 0.6f));

        dense.anchoredPosition = Vector2.zero;
        dense.localScale = Vector3.one * Mathf.Lerp(1.5f, 1.1f, t);
        SetAlpha(denseImage, t);

        midLeft.anchoredPosition = new Vector2(Mathf.Lerp(-1400f, -250f, t), 0f);
        midRight.anchoredPosition = new Vector2(Mathf.Lerp(1400f, 250f, t), 0f);
        SetAlpha(midLeftImage, t);
        SetAlpha(midRightImage, t);

        thin.anchoredPosition = new Vector2(Mathf.Lerp(-300f, 0f, t), 0f);
        SetAlpha(thinImage, t * 0.8f);

        // 안개가 거의 덮였을 때 제목이 떠오름
        titleGroup.alpha = Mathf.Clamp01((t - 0.6f) / 0.4f);
    }

    // 걷기: 0(완전히 덮임) ~ 1(모두 걷힘). 앞쪽 짙은 안개가 먼저 흩어지고 양옆 덩어리가 밀려나며 옅은 띠가 마지막에 사라짐
    private void SetClear(float t)
    {
        SetAlpha(back, 1f - Mathf.Clamp01(t / 0.3f));

        float d = Mathf.Clamp01(t / 0.6f);
        dense.localScale = Vector3.one * Mathf.Lerp(1.1f, 1.8f, EaseOut(d));
        SetAlpha(denseImage, 1f - d);

        float m = Mathf.Clamp01((t - 0.1f) / 0.8f);
        float push = EaseIn(m);
        midLeft.anchoredPosition = new Vector2(Mathf.Lerp(-250f, -1900f, push), 0f);
        midRight.anchoredPosition = new Vector2(Mathf.Lerp(250f, 1900f, push), 0f);
        SetAlpha(midLeftImage, 1f - m);
        SetAlpha(midRightImage, 1f - m);

        float th = Mathf.Clamp01((t - 0.3f) / 0.7f);
        thin.anchoredPosition = new Vector2(Mathf.Lerp(0f, 600f, th), 0f);
        SetAlpha(thinImage, 0.8f * (1f - th));

        // 제목은 안개가 걷히기 시작하면서 살짝 떠오르며 사라짐
        float title = Mathf.Clamp01(t / 0.35f);
        titleGroup.alpha = 1f - title;
        titleGroup.transform.localPosition = new Vector3(0f, Mathf.Lerp(0f, 40f, title), 0f);

        // 반쯤 걷히면 맵을 누를 수 있게
        back.raycastTarget = t < 0.5f;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    // 씬 로딩 직후 긴 프레임 때문에 연출이 건너뛰어지지 않도록 한 프레임 시간을 제한
    private static float StepTime()
    {
        return Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseIn(float t) => t * t;
}
