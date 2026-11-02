using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 랭킹 UI를 코드로 만드는 도우미 (로비 랭킹 버튼이 씬에 없을 때 실행 중에 만들어 붙이는 용도)
public static class RankingUIFactory
{
    public static readonly Color ButtonColor = new Color(0.05f, 0.03f, 0.03f, 0.95f);
    public static readonly Color AccentColor = new Color(0.85f, 0.12f, 0.1f);

    // 랭킹 팝업을 여는 "RANKING" 버튼을 만들어 붙인다
    public static Button CreateOpenButton(Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize)
    {
        Button button = CreateStyledButton(parent, "RankingButton", "RANKING", size, fontSize);

        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;

        button.onClick.AddListener(RankingPanel.Open);

        return button;
    }

    // 랭킹 UI 공통 버튼 모양 (검은 배경 + 빨간 글씨 / 테두리)
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

    // 부모 영역에 꽉 차게
    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
