using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전투 씬에 전투 로그 패널(BattleLogPanel)을 만들고, 주사위 굴리기 / 턴 종료 버튼을 아래로 내리는 에디터 툴.
// - 패널은 버튼이 있던 자리(화면 오른쪽, 적 아래)에 놓고, 위 변 기준이라 펼치면 아래로 늘어난다.
// - 맨 위에 검은 바(잡고 끌어서 이동)를 두고, 바 오른쪽에 펼치기 ▼ / 줄이기 ▲ 버튼을 붙인다.
// - 이미 패널이 있으면 지우고 새로 만든다 (위치/크기는 아래 상수로 조정).
// 사용법: Tools > Battle > 전투 로그 UI 배치 (전투 씬) 실행. 전투 씬을 열어 배치하고 저장한다.
public static class BattleLogUISetup
{
    private const string ScenePath = "Assets/Workspaces/Junha/Scene/junhaTest.unity";
    private const string FontPath = "Assets/Workspaces/Junha/Resources/온글잎 콘콘체 SDF.asset";
    private const string PanelName = "BattleLogPanel";

    // 기준 해상도 1920x1080, 화면 중앙 기준 좌표.
    // 버튼은 원래 (780, -251)에 있었고, 오른쪽 아래 버림 더미 바로 위까지 내린다.
    private static readonly Vector2 ButtonPos = new Vector2(780f, -365f);

    // 패널 위 변 중앙 위치 (펼쳤을 때 아래 변이 버튼 위 10px에 오도록)
    private static readonly Vector2 PanelPos = new Vector2(780f, -105f);
    private const float PanelWidth = 330f;
    private const float ExpandedHeight = 220f; // 바 + 6줄 정도. 줄이면 바 높이만 남음
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
    private const float Padding = 10f;
    private const float FontSize = 22f;

    // 위쪽 검은 바 (잡고 끌어서 이동)
    private const float BarHeight = 32f;
    private static readonly Color BarColor = new Color(0f, 0f, 0f, 0.9f);
    private const float BarFontSize = 18f;
    private static readonly Color BarTextColor = new Color(0.75f, 0.75f, 0.75f);
    private const float ToggleWidth = 40f;

    // 크기 조절 영역 (펼쳤을 때만 보임)
    private const float EdgeThickness = 8f;
    private const float CornerSize = 14f;

    [MenuItem("Tools/Battle/전투 로그 UI 배치 (전투 씬)")]
    private static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string originalScene = SceneManager.GetActiveScene().path;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (SetupScene())
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BattleLogUISetup] 배치 완료: {ScenePath}");
        }

        if (!string.IsNullOrEmpty(originalScene) && originalScene != ScenePath)
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
    }

    private static bool SetupScene()
    {
        DiceManager dice = FindInScene<DiceManager>();
        if (dice == null || dice.rollDiceButton == null)
        {
            Debug.LogError("[BattleLogUISetup] DiceManager 또는 rollDiceButton을 찾을 수 없습니다.");
            return false;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError($"[BattleLogUISetup] 글꼴을 찾을 수 없습니다: {FontPath}");
            return false;
        }

        RectTransform rollButton = (RectTransform)dice.rollDiceButton.transform;
        Transform canvas = rollButton.parent;

        // 버튼 내리기 (턴 종료 버튼은 실행 시 주사위 버튼 위치로 맞춰지지만 씬에서도 같이 옮겨 둠)
        rollButton.anchoredPosition = ButtonPos;
        EditorUtility.SetDirty(rollButton);

        TurnManager turnManager = FindInScene<TurnManager>();
        Button endTurn = turnManager != null ? turnManager.endTurnButton : null;
        if (endTurn == null)
        {
            Transform found = canvas.Find("EndTurnButton");
            if (found != null) endTurn = found.GetComponent<Button>();
        }
        if (endTurn != null)
        {
            ((RectTransform)endTurn.transform).anchoredPosition = ButtonPos;
            EditorUtility.SetDirty(endTurn.transform);
        }
        else
        {
            Debug.LogWarning("[BattleLogUISetup] EndTurnButton을 찾지 못해 위치를 옮기지 않았습니다.");
        }

        Transform old = canvas.Find(PanelName);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // 패널: 위 변 기준이라 높이를 늘리면 아래로 펼쳐짐
        RectTransform panel = CreateUI(PanelName, canvas);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = PanelPos;
        panel.sizeDelta = new Vector2(PanelWidth, ExpandedHeight);
        // EnemyHitbox(투명 Image)보다 위에 그려야 바를 잡을 수 있음. 보상 / 설정 창보다는 아래.
        // (카드를 적에게 놓는 판정은 Physics2D라서 패널이 위에 있어도 영향 없음)
        Transform reward = canvas.Find("RewardPanel");
        if (reward != null) panel.SetSiblingIndex(reward.GetSiblingIndex());
        else Debug.LogWarning("[BattleLogUISetup] RewardPanel을 찾지 못해 로그 패널을 맨 위에 그립니다.");

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = PanelColor;

        ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 30f;

        RectTransform viewport = CreateUI("Viewport", panel);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(Padding, Padding);
        viewport.offsetMax = new Vector2(-Padding, -(BarHeight + Padding));
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform textRect = CreateUI("LogText", viewport);
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI logText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        logText.font = font;
        logText.fontSize = FontSize;
        logText.color = Color.white;
        logText.richText = true;
        logText.textWrappingMode = TextWrappingModes.Normal;
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.raycastTarget = false;
        logText.text = "";

        ContentSizeFitter fitter = textRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = textRect;

        // 위쪽 검은 바: 잡고 끌면 패널이 따라 움직임
        RectTransform bar = CreateUI("HeaderBar", panel);
        bar.anchorMin = new Vector2(0f, 1f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = Vector2.zero;
        bar.sizeDelta = new Vector2(0f, BarHeight);
        bar.gameObject.AddComponent<Image>().color = BarColor;

        // 펼치기 ▼ / 줄이기 ▲ 버튼: 바 오른쪽 끝
        RectTransform toggle = CreateUI("ToggleButton", bar);
        toggle.anchorMin = new Vector2(1f, 0f);
        toggle.anchorMax = new Vector2(1f, 1f);
        toggle.pivot = new Vector2(1f, 0.5f);
        toggle.anchoredPosition = Vector2.zero;
        toggle.sizeDelta = new Vector2(ToggleWidth, 0f);

        // 투명 이미지로 클릭 영역만 잡음 (누르는 동안 살짝 밝아짐)
        Image toggleImage = toggle.gameObject.AddComponent<Image>();
        toggleImage.color = new Color(1f, 1f, 1f, 0f);
        Button toggleButton = toggle.gameObject.AddComponent<Button>();
        toggleButton.targetGraphic = toggleImage;
        ColorBlock colors = toggleButton.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.3f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        toggleButton.colors = colors;

        RectTransform labelRect = CreateUI("Label", toggle);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = CreateText(labelRect, font, "▼", TextAlignmentOptions.Center);

        BattleLogUI ui = panel.gameObject.AddComponent<BattleLogUI>();
        bar.gameObject.AddComponent<BattleLogDragBar>().logUI = ui;

        // 크기 조절 영역: 왼쪽/오른쪽/아래 변 + 아래 두 모서리 (모두 투명)
        RectTransform handles = CreateUI("ResizeHandles", panel);
        handles.anchorMin = Vector2.zero;
        handles.anchorMax = Vector2.one;
        handles.offsetMin = handles.offsetMax = Vector2.zero;

        CreateResizeHandle(handles, ui, "Left", -1, 0,
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(EdgeThickness, -BarHeight), Color.clear);
        CreateResizeHandle(handles, ui, "Right", 1, 0,
            new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-EdgeThickness, 0f), new Vector2(0f, -BarHeight), Color.clear);
        CreateResizeHandle(handles, ui, "Bottom", 0, -1,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, EdgeThickness), Color.clear);
        CreateResizeHandle(handles, ui, "BottomLeft", -1, -1,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(CornerSize, CornerSize), Color.clear);
        CreateResizeHandle(handles, ui, "BottomRight", 1, -1,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-CornerSize, 0f), new Vector2(0f, CornerSize), Color.clear);
        ui.panel = panel;
        ui.scrollRect = scroll;
        ui.logText = logText;
        ui.toggleButton = toggleButton;
        ui.toggleLabel = label;
        ui.resizeHandles = handles.gameObject;
        ui.barHeight = BarHeight;
        ui.minExpandedSize = new Vector2(PanelWidth, ExpandedHeight);
        EditorUtility.SetDirty(ui);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        return true;
    }

    // offsetMin / offsetMax 는 앵커 기준 왼쪽아래 / 오른쪽위 여백
    private static void CreateResizeHandle(RectTransform parent, BattleLogUI ui, string name, int horizontal, int vertical,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        RectTransform rect = CreateUI(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        // 투명해도 Image가 있어야 마우스 입력을 받음
        rect.gameObject.AddComponent<Image>().color = color;

        BattleLogResizeHandle handle = rect.gameObject.AddComponent<BattleLogResizeHandle>();
        handle.logUI = ui;
        handle.horizontal = horizontal;
        handle.vertical = vertical;
    }

    private static TextMeshProUGUI CreateText(RectTransform rect, TMP_FontAsset font, string text, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = BarFontSize;
        tmp.color = BarTextColor;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.text = text;
        return tmp;
    }

    private static RectTransform CreateUI(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }

    private static T FindInScene<T>() where T : Object
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return found.Length > 0 ? found[0] : null;
    }
}
