using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 로비 분위기 연출 (LobbyScene/Canvas/LobbyAmbience 에 붙어 있음, 배경 바로 위·로고/버튼 아래 층)
//   1) 들어올 때: 로고가 위에서 살짝 내려앉으며 서서히 나타나고, 메뉴 버튼이 차례로 미끄러져 들어옴
//   2) 상시: 로고가 은은하게 숨쉬듯 커졌다 작아지고 밝기가 오르내림
//   3) 화면 가장자리를 어둡게 (비네트) → 가운데 로고/메뉴로 시선 집중
//   4) 아래에서 떠오르는 불씨 (붉은 불씨 + 횃불 색에 맞춘 푸른 불씨 조금)
// 이미지 에셋 없이 코드로 만든 부드러운 원/그라데이션 텍스처를 사용. 실제 시간 기준이라 timeScale과 무관.
public class LobbyAmbience : MonoBehaviour
{
    [Header("연결 (비우면 해당 연출 생략)")]
    [SerializeField] private RectTransform logo;
    [SerializeField] private RectTransform[] menuButtons;

    [Header("등장 연출")]
    [SerializeField] private float logoIntroDelay = 0.15f;
    [SerializeField] private float logoIntroTime = 1.2f;
    [SerializeField] private float logoDropDistance = 40f;
    [SerializeField] private float buttonsStartDelay = 0.7f;
    [SerializeField] private float buttonStagger = 0.12f;
    [SerializeField] private float buttonIntroTime = 0.45f;
    [SerializeField] private float buttonSlideDistance = 60f;

    [Header("로고 숨쉬기")]
    [SerializeField] private float breatheScale = 0.015f;
    [SerializeField] private float breatheSpeed = 0.35f; // 1초에 몇 번
    [SerializeField, Range(0f, 0.3f)] private float breatheDim = 0.1f;

    [Header("비네트 (화면 가장자리 어둡게)")]
    [SerializeField] private bool useVignette = true;
    [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.75f;

    [Header("불씨")]
    [SerializeField] private int emberCount = 60;
    [SerializeField] private Vector2 emberSize = new Vector2(5f, 14f);
    [SerializeField] private Vector2 emberSpeed = new Vector2(35f, 95f);   // 위로 올라가는 속도 (px/초)
    [SerializeField] private Vector2 emberLife = new Vector2(4f, 9f);
    [SerializeField] private Color emberColor = new Color(1f, 0.35f, 0.12f, 1f);
    [SerializeField] private Color emberColorAlt = new Color(0.45f, 0.6f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float altColorChance = 0.18f;

    private class Ember
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos;
        public float speed, life, age, size, swayAmp, swayFreq, phase, flicker;
        public Color color;
    }

    private readonly List<Ember> embers = new List<Ember>();
    private RectTransform area;
    private Sprite dotSprite;
    private Sprite vignetteSprite;

    private Vector2 logoBasePos;
    private Vector3 logoBaseScale;
    private Graphic logoGraphic;
    private Color logoBaseColor;
    private Vector2[] buttonBasePos;
    private Graphic[] buttonGraphics;
    private Color[] buttonBaseColors;
    private float startTime;

    private void Start()
    {
        area = (RectTransform)transform;
        startTime = Time.unscaledTime;

        if (useVignette) CreateVignette();
        if (emberCount > 0) CreateEmbers();

        if (logo != null)
        {
            logoBasePos = logo.anchoredPosition;
            logoBaseScale = logo.localScale;
            logoGraphic = logo.GetComponent<Graphic>();
            if (logoGraphic != null) logoBaseColor = logoGraphic.color;
        }

        if (menuButtons != null)
        {
            buttonBasePos = new Vector2[menuButtons.Length];
            buttonGraphics = new Graphic[menuButtons.Length];
            buttonBaseColors = new Color[menuButtons.Length];
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] == null) continue;
                buttonBasePos[i] = menuButtons[i].anchoredPosition;
                buttonGraphics[i] = menuButtons[i].GetComponent<Graphic>();
                if (buttonGraphics[i] != null) buttonBaseColors[i] = buttonGraphics[i].color;
            }
        }

        UpdateIntro(0f);
    }

    private void Update()
    {
        float t = Time.unscaledTime - startTime;
        UpdateIntro(t);
        UpdateEmbers(Time.unscaledDeltaTime);
    }

    // ===================== 등장 + 로고 숨쉬기 =====================

    private static float EaseOut(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - (1f - x) * (1f - x) * (1f - x);
    }

    private void UpdateIntro(float t)
    {
        if (logo != null)
        {
            float k = EaseOut((t - logoIntroDelay) / Mathf.Max(0.01f, logoIntroTime));
            float breathe = Mathf.Sin(t * Mathf.PI * 2f * breatheSpeed);
            logo.anchoredPosition = logoBasePos + new Vector2(0f, (1f - k) * logoDropDistance);
            logo.localScale = logoBaseScale * (1f + breathe * breatheScale);
            if (logoGraphic != null)
            {
                float bright = 1f - breatheDim * (0.5f - 0.5f * breathe);
                Color c = logoBaseColor;
                c.r *= bright; c.g *= bright; c.b *= bright;
                c.a = logoBaseColor.a * k;
                logoGraphic.color = c;
            }
        }

        if (menuButtons == null || buttonBasePos == null) return;
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null) continue;
            float k = EaseOut((t - buttonsStartDelay - i * buttonStagger) / Mathf.Max(0.01f, buttonIntroTime));
            menuButtons[i].anchoredPosition = buttonBasePos[i] + new Vector2(0f, -(1f - k) * buttonSlideDistance);
            if (buttonGraphics[i] != null)
            {
                Color c = buttonBaseColors[i];
                c.a *= k;
                buttonGraphics[i].color = c;
            }
        }
    }

    // ===================== 비네트 =====================

    private void CreateVignette()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.85f + dy * dy * 1.1f);   // 가로로 조금 넓은 타원
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.25f, d));
                px[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        Image img = CreateImage("Vignette", vignetteSprite);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        img.color = new Color(1f, 1f, 1f, vignetteStrength);
    }

    // ===================== 불씨 =====================

    private void CreateEmbers()
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color32[] px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Pow(1f - d, 2.2f);                 // 가운데가 밝고 바깥으로 부드럽게 사라지는 점
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        for (int i = 0; i < emberCount; i++)
        {
            Ember e = new Ember();
            e.img = CreateImage("Ember", dotSprite);
            e.rt = e.img.rectTransform;
            Respawn(e, true);
            embers.Add(e);
        }
    }

    private void Respawn(Ember e, bool anywhere)
    {
        Rect r = area.rect;
        e.size = Random.Range(emberSize.x, emberSize.y);
        e.speed = Random.Range(emberSpeed.x, emberSpeed.y);
        e.life = Random.Range(emberLife.x, emberLife.y);
        e.age = anywhere ? Random.Range(0f, e.life) : 0f;
        e.swayAmp = Random.Range(8f, 30f);
        e.swayFreq = Random.Range(0.2f, 0.6f);
        e.phase = Random.Range(0f, Mathf.PI * 2f);
        e.flicker = Random.Range(4f, 9f);
        e.color = Random.value < altColorChance ? emberColorAlt : emberColor;
        e.pos = new Vector2(Random.Range(r.xMin, r.xMax), r.yMin - 10f + (anywhere ? e.speed * e.age : 0f));
        e.rt.sizeDelta = new Vector2(e.size, e.size);
    }

    private void UpdateEmbers(float dt)
    {
        if (embers.Count == 0) return;
        Rect r = area.rect;
        for (int i = 0; i < embers.Count; i++)
        {
            Ember e = embers[i];
            e.age += dt;
            if (e.age >= e.life || e.pos.y > r.yMax + 20f)
            {
                Respawn(e, false);
                continue;
            }

            e.pos.y += e.speed * dt;
            float sway = Mathf.Sin(e.age * Mathf.PI * 2f * e.swayFreq + e.phase) * e.swayAmp;
            e.rt.anchoredPosition = new Vector2(e.pos.x + sway, e.pos.y);

            float p = e.age / e.life;
            float fade = Mathf.Clamp01(p / 0.15f) * Mathf.Clamp01((1f - p) / 0.35f);   // 서서히 나타났다 사라짐
            float flick = 0.75f + 0.25f * Mathf.Sin(e.age * e.flicker + e.phase);
            Color c = e.color;
            c.a = fade * flick;
            e.img.color = c;
        }
    }

    private Image CreateImage(string objName, Sprite sprite)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private void OnDestroy()
    {
        foreach (Sprite sp in new[] { dotSprite, vignetteSprite })
        {
            if (sp == null) continue;
            Destroy(sp.texture);
            Destroy(sp);
        }
    }
}
