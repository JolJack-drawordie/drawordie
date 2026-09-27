using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 랭킹 팝업 씬(RankingScene)을 만들고 Build Settings에 등록하는 1회성 에디터 툴.
// 사용법: Tools > Ranking > 랭킹 씬 생성 실행. 생성된 씬은 자유롭게 수정해도 된다. (RankingPanel의 참조만 유지)
//        이미 만든 씬의 Box / Title만 프리팹으로 바꾸려면 RankingScene을 연 상태에서
//        Tools > Ranking > 랭킹 창 Box·Title 프리팹으로 교체 실행.
public static class RankingSceneSetup
{
    private const string ScenePath = "Assets/Workspaces/Junha/Scene/RankingScene.unity";
    private const string BackgroundPrefabPath = "Assets/Workspaces/Junha/Prefabs/Background.prefab";
    private const string HeaderPrefabPath = "Assets/Workspaces/Junha/Prefabs/Header (Bloodlines).prefab";

    [MenuItem("Tools/Ranking/랭킹 씬 생성")]
    private static void Setup()
    {
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("랭킹 씬 생성", $"{ScenePath} 가 이미 있습니다. 덮어쓸까요?", "덮어쓰기", "취소"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RankingPanel panel = BuildPanel();
        ReplaceBoxAndTitle(panel);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);

        Debug.Log($"<color=green>[RankingSceneSetup] 랭킹 씬 생성 완료: {ScenePath}</color>");
    }

    [MenuItem("Tools/Ranking/랭킹 창 Box·Title 프리팹으로 교체")]
    private static void ReplaceBoxAndTitleInOpenScene()
    {
        RankingPanel panel = Object.FindFirstObjectByType<RankingPanel>();
        if (panel == null)
        {
            Debug.LogError("[RankingSceneSetup] 씬에서 RankingPanel을 찾을 수 없습니다. RankingScene을 열어주세요.");
            return;
        }

        Undo.SetCurrentGroupName("랭킹 창 Box·Title 프리팹으로 교체");
        int undoGroup = Undo.GetCurrentGroup();

        if (!ReplaceBoxAndTitle(panel)) return;

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        Debug.Log("<color=green>[RankingSceneSetup] Box → Background, Title → Header 프리팹 교체 완료! 씬을 저장하세요.</color>");
    }

    // Box → Background 프리팹, Title → Header (Bloodlines) 프리팹으로 교체 (위치/크기/자식 유지)
    private static bool ReplaceBoxAndTitle(RankingPanel panel)
    {
        GameObject backgroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
        GameObject headerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeaderPrefabPath);
        if (backgroundPrefab == null || headerPrefab == null)
        {
            Debug.LogError($"[RankingSceneSetup] 프리팹을 찾을 수 없습니다: {BackgroundPrefabPath}, {HeaderPrefabPath}");
            return false;
        }

        Transform box = FindChild(panel.transform, "Box");
        Transform title = FindChild(panel.transform, "Title");
        if (box == null && title == null)
        {
            Debug.LogWarning("[RankingSceneSetup] Box / Title 오브젝트가 없습니다. 이미 교체된 것 같습니다.");
            return false;
        }

        if (box != null)
            ReplaceWithPrefab(box, backgroundPrefab);

        if (title != null)
        {
            GameObject header = ReplaceWithPrefab(title, headerPrefab);
            TMP_Text headerText = header.GetComponent<TMP_Text>();
            if (headerText != null)
            {
                Undo.RecordObject(headerText, "Header 텍스트 변경");
                headerText.text = "RANKING";
            }
        }

        return true;
    }

    // 기존 오브젝트 자리에 프리팹을 배치하고, 위치/크기와 자식을 옮긴 뒤 기존 오브젝트를 삭제
    private static GameObject ReplaceWithPrefab(Transform target, GameObject prefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target.parent);
        Undo.RegisterCreatedObjectUndo(instance, "프리팹 배치: " + prefab.name);
        instance.transform.SetSiblingIndex(target.GetSiblingIndex());

        RectTransform from = (RectTransform)target;
        RectTransform to = (RectTransform)instance.transform;
        to.anchorMin = from.anchorMin;
        to.anchorMax = from.anchorMax;
        to.pivot = from.pivot;
        to.anchoredPosition = from.anchoredPosition;
        to.sizeDelta = from.sizeDelta;
        to.localScale = Vector3.one;
        to.localRotation = Quaternion.identity;

        while (target.childCount > 0)
            Undo.SetTransformParent(target.GetChild(0), instance.transform, false, "자식 이동");

        Undo.DestroyObjectImmediate(target.gameObject);
        return instance;
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }

    private static RankingPanel BuildPanel()
    {
        // Canvas (AuthScene처럼 EventSystem은 두지 않고 아래 씬의 것을 사용)
        GameObject canvasObj = new GameObject("RankingCanvas", typeof(RectTransform));

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100; // 플레이 타임 표시(1000)보다 위

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 화면 전체를 어둡게 하고 아래 화면 클릭을 막음
        GameObject dim = RankingUIFactory.CreateImage(canvasObj.transform, "Dim", new Color(0f, 0f, 0f, 0.75f));
        RankingUIFactory.Stretch((RectTransform)dim.transform);

        // 랭킹 창
        GameObject box = RankingUIFactory.CreateImage(dim.transform, "Box", new Color(0.08f, 0.06f, 0.06f, 0.97f));
        ((RectTransform)box.transform).sizeDelta = new Vector2(1300f, 880f);

        // 제목
        TextMeshProUGUI title = RankingUIFactory.CreateText(box.transform, "Title", "RANKING", 64f, RankingUIFactory.AccentColor);
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        RankingUIFactory.SetTopArea(title.rectTransform, -30f, 80f);

        // 최근 내 기록
        TextMeshProUGUI myRankText = RankingUIFactory.CreateText(box.transform, "MyRank", "", 30f, new Color(1f, 0.84f, 0.29f));
        myRankText.alignment = TextAlignmentOptions.Center;
        RankingUIFactory.SetTopArea(myRankText.rectTransform, -115f, 40f);

        // 열 제목 (목록과 같은 좌우 여백 60)
        TextMeshProUGUI header = RankingUIFactory.CreateText(box.transform, "Header",
            "순위<pos=8%>닉네임<pos=35%>진행도<pos=55%>점수<pos=71%>시간<pos=84%>날짜",
            28f, new Color(0.65f, 0.65f, 0.65f));
        header.alignment = TextAlignmentOptions.Left;
        RankingUIFactory.SetTopArea(header.rectTransform, -170f, 40f, 60f);

        // 목록 (스크롤)
        GameObject viewport = RankingUIFactory.CreateImage(box.transform, "Viewport", new Color(0f, 0f, 0f, 0f));
        viewport.AddComponent<RectMask2D>();
        RectTransform viewportRect = (RectTransform)viewport.transform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(60f, 120f);
        viewportRect.offsetMax = new Vector2(-60f, -220f);

        TextMeshProUGUI listText = RankingUIFactory.CreateText(viewport.transform, "List", "", 30f, Color.white);
        listText.alignment = TextAlignmentOptions.TopLeft;
        listText.lineSpacing = 25f;
        RectTransform listRect = listText.rectTransform;
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.anchoredPosition = Vector2.zero;
        listRect.sizeDelta = Vector2.zero;

        ContentSizeFitter fitter = listText.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = listRect;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;

        // 닫기 버튼
        Button closeButton = RankingUIFactory.CreateStyledButton(box.transform, "CloseButton", "닫기", new Vector2(220f, 65f), 32f);
        RectTransform closeRect = (RectTransform)closeButton.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 35f);

        // RankingPanel 연결
        RankingPanel panel = canvasObj.AddComponent<RankingPanel>();
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("myRankText").objectReferenceValue = myRankText;
        so.FindProperty("listText").objectReferenceValue = listText;
        so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        return panel;
    }

    private static void AddToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[RankingSceneSetup] Build Settings에 {path} 추가");
    }
}
