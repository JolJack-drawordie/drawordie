using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 랭킹 UI 생성 도우미. 로비/클리어 화면의 랭킹 버튼(실행 중)과 랭킹 씬 생성 에디터 툴에서 함께 사용한다.
public static class RankingUIFactory
{
    public static readonly Color ButtonColor = new Color(0.05f, 0.03f, 0.03f, 0.95f);
    public static readonly Color AccentColor = new Color(0.85f, 0.12f, 0.1f);

    // 다른 화면(로비, 클리어 화면)에 랭킹 팝업을 여는 "RANKING" 버튼을 만들어 붙인다.
    public static Button CreateOpenButton(Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        Button button = CreateStyledButton(parent, "RankingButton", "RANKING", size, fontSize);

        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;

        button.onClick.AddListener(RankingPanel.Open);

        return button;
    }

    // 랭킹 UI 공통 버튼 스타일 (검은 배경 + 빨간 글씨/테두리)
    public static Button CreateStyledButton(Transform parent, string name, string label, Vector2 size, float fontSize)
    {
        GameObject obj = CreateImage(parent, name, ButtonColor);
        ((RectTransform)obj.transform).sizeDelta = size;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = AccentColor;
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.8f, 0.8f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText(obj.transform, "Label", label, fontSize, AccentColor);
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform);

        return button;
    }

    public static GameObject CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        Image image = obj.AddComponent<Image>();
        image.color = color;

        return obj;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.raycastTarget = false;

        return tmp;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // 부모 상단 기준으로 가로로 꽉 찬 영역 배치
    public static void SetTopArea(RectTransform rect, float top, float height, float sidePadding = 0f)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, top);
        rect.sizeDelta = new Vector2(-sidePadding * 2f, height);
    }
}
