using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 적 머리 위 의도 아이콘에 마우스를 올리면 아이콘 위쪽에 뜨는 설명창 (검은 배경 + 온글잎 콘콘체).
// 화면 맨 위에 그려지는 전용 Canvas를 처음 쓸 때 만들어 두고 같이 쓴다.
public static class IntentTooltip
{
    private const string FontResourcePath = "온글잎 콘콘체 SDF";
    private const float Gap = 12f;
    private const float ScreenPadding = 10f;

    private static RectTransform canvasRect;
    private static RectTransform panel;
    private static TextMeshProUGUI text;
    private static object owner;

    // iconRect: 의도 아이콘, cam: 아이콘이 있는 월드 스페이스 Canvas를 그리는 카메라
    public static void Show(object requester, string content, RectTransform iconRect, Camera cam)
    {
        if (string.IsNullOrEmpty(content) || iconRect == null) { Hide(requester); return; }

        EnsureCreated();
        owner = requester;

        if (text.text != content)
        {
            text.text = content;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        panel.gameObject.SetActive(true);

        // 아이콘 위쪽 가운데 / 아래쪽 가운데 화면 좌표
        Vector3[] corners = new Vector3[4];
        iconRect.GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2((bottomLeft.x + topRight.x) * 0.5f, topRight.y), null, out Vector2 top);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2((bottomLeft.x + topRight.x) * 0.5f, bottomLeft.y), null, out Vector2 bottom);

        Rect bounds = canvasRect.rect;
        Vector2 size = panel.rect.size;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        // 기본은 아이콘 위, 위쪽 공간이 부족하면 아이콘 아래
        float y = top.y + Gap + halfH;
        if (y + halfH > bounds.yMax - ScreenPadding) y = bottom.y - Gap - halfH;

        float x = Mathf.Clamp(top.x, bounds.xMin + halfW + ScreenPadding, bounds.xMax - halfW - ScreenPadding);
        y = Mathf.Clamp(y, bounds.yMin + halfH + ScreenPadding, bounds.yMax - halfH - ScreenPadding);

        panel.anchoredPosition = new Vector2(x, y);
    }

    // 다른 적의 툴팁을 실수로 끄지 않도록, 띄운 쪽만 끌 수 있음
    public static void Hide(object requester)
    {
        if (panel == null || owner != requester) return;
        panel.gameObject.SetActive(false);
        owner = null;
    }

    private static void EnsureCreated()
    {
        if (panel != null) return;

        GameObject canvasObj = new GameObject("IntentTooltipCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObj.GetComponent<RectTransform>();

        GameObject panelObj = new GameObject("IntentTooltip", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel = panelObj.GetComponent<RectTransform>();
        panel.SetParent(canvasRect, false);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);

        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = false;

        VerticalLayoutGroup layout = panelObj.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 10, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObj.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(panel, false);
        text = textObj.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(FontResourcePath);
        if (font != null) text.font = font;
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.richText = true;
        text.raycastTarget = false;

        panelObj.SetActive(false);
    }
}
