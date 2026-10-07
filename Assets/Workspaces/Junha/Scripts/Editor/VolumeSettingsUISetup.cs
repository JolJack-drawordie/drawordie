using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 설정 패널(SettingsPanel)을 배경음 / 효과음 / 밝기 3줄로 배치하고 SettingsManager에 연결하는 에디터 툴.
// 맨 위에 "설정" 제목, 각 줄: [이름 라벨] [슬라이더] [% 라벨] [소리 아이콘 토글]   (밝기 줄은 아이콘 없음)
// - 기존 SoundSlider는 배경음 슬라이더로 쓰고, 복제해서 효과음 슬라이더를 만든다.
// - 아이콘은 Bloodlines UI의 Icon-Toggle 1 프리팹. 꺼짐 = 회색 음소거 스피커(Icons-2), 켜짐 = 빨간 스피커.
// - 이미 만들어진 요소는 다시 만들지 않고 위치만 맞춘다 (여러 번 실행해도 됨).
// 사용법: Tools > Settings > 음량 UI 배치 (로비/휴식/전투 씬) 실행. 세 씬을 차례로 열어 배치하고 저장한다.
public static class VolumeSettingsUISetup
{
    private static readonly string[] ScenePaths =
    {
        "Assets/Workspaces/Taeyoon/Scenes/LobbyScene.unity",
        "Assets/Workspaces/Seonggu/Scenes/RestScene.unity",
        "Assets/Workspaces/Junha/Scene/junhaTest.unity",
    };

    private const string TogglePrefabPath =
        "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Prefabs/Icon/Icon-Toggle 1.prefab";
    private const string MutedIconPath =
        "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Textures/Icon/Icons-2.png";

    // 패널 크기 800x500 기준 (중앙 앵커)
    private const float TitleY = 205f;
    private static readonly Vector2 TitleSize = new Vector2(400f, 60f);
    private const float TitleFontSize = 44f;

    private const float BgmRowY = 150f;
    private const float SfxRowY = 90f;
    private const float BrightnessRowY = 30f;

    private const float NameX = -290f;
    private const float SliderX = 5f;
    private const float ValueX = 265f;
    private const float IconX = 345f;
    private static readonly Vector2 NameSize = new Vector2(140f, 40f);
    private static readonly Vector2 SliderSize = new Vector2(420f, 30f);
    private static readonly Vector2 ValueSize = new Vector2(80f, 40f);
    private static readonly Vector2 IconSize = new Vector2(50f, 50f);
    private const float LabelFontSize = 28f;

    private const string UndoName = "음량 UI 배치";

    [MenuItem("Tools/Settings/음량 UI 배치 (로비, 휴식, 전투 씬)")]
    private static void SetupAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string originalScene = SceneManager.GetActiveScene().path;

        foreach (string path in ScenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (SetupScene())
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[VolumeSettingsUISetup] 배치 완료: {path}");
            }
        }

        if (!string.IsNullOrEmpty(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
    }

    private static bool SetupScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        SettingsManager settings = FindInScene<SettingsManager>();
        if (settings == null)
        {
            Debug.LogError($"[VolumeSettingsUISetup] {sceneName}: SettingsManager를 찾을 수 없습니다.");
            return false;
        }

        SerializedObject so = new SerializedObject(settings);

        GameObject panel = so.FindProperty("settingsPanel").objectReferenceValue as GameObject;
        if (panel == null)
        {
            Debug.LogError($"[VolumeSettingsUISetup] {sceneName}: settingsPanel 참조가 비어 있습니다.");
            return false;
        }
        Transform root = panel.transform;

        Slider bgmSlider = GetRef<Slider>(so, "bgmSlider", root, "BGMSlider", "SoundSlider");
        Slider brightnessSlider = GetRef<Slider>(so, "brightnessSlider", root, "BrightnessSlider");
        if (bgmSlider == null || brightnessSlider == null)
        {
            Debug.LogError($"[VolumeSettingsUISetup] {sceneName}: 음량/밝기 슬라이더를 찾을 수 없습니다.");
            return false;
        }

        GameObject togglePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TogglePrefabPath);
        Sprite mutedIcon = AssetDatabase.LoadAssetAtPath<Sprite>(MutedIconPath);
        if (togglePrefab == null || mutedIcon == null)
        {
            Debug.LogError("[VolumeSettingsUISetup] 아이콘 토글 프리팹이나 음소거 아이콘을 찾을 수 없습니다.");
            return false;
        }

        // 라벨 글꼴: 이미 있는 라벨 → 없으면 패널의 버튼 글씨(닫기 버튼 등)
        TextMeshProUGUI fontSource =
            GetRef<TextMeshProUGUI>(so, "bgmLabelText", root, "BGMLabel") ??
            root.GetComponentInChildren<TextMeshProUGUI>(true);

        // 제목
        TextMeshProUGUI title = GetOrCreateLabel(so, null, root, "TitleText", fontSource, "설정");
        Place(title.transform, 0f, TitleY, TitleSize);
        Undo.RecordObject(title, UndoName);
        title.fontSize = TitleFontSize;
        title.alignment = TextAlignmentOptions.Center;

        // 효과음 슬라이더가 없으면 배경음 슬라이더를 복제
        Slider sfxSlider = GetRef<Slider>(so, "sfxSlider", root, "SFXSlider");
        if (sfxSlider == null)
        {
            GameObject sfxObj = Object.Instantiate(bgmSlider.gameObject, root);
            sfxObj.transform.SetSiblingIndex(bgmSlider.transform.GetSiblingIndex() + 1);
            Undo.RegisterCreatedObjectUndo(sfxObj, UndoName);
            sfxSlider = sfxObj.GetComponent<Slider>();
        }
        bgmSlider.gameObject.name = "BGMSlider";
        sfxSlider.gameObject.name = "SFXSlider";

        // 배경음 줄
        Place(bgmSlider.transform, SliderX, BgmRowY, SliderSize);
        TextMeshProUGUI bgmName = GetOrCreateLabel(so, "bgmLabelText", root, "BGMLabel", fontSource, "배경음");
        TextMeshProUGUI bgmValue = GetOrCreateLabel(so, "bgmValueText", root, "BGMValue", fontSource, "100%");
        PlaceName(bgmName, BgmRowY);
        PlaceValue(bgmValue, BgmRowY);
        Toggle bgmToggle = GetOrCreateToggle(so, "bgmToggle", root, "BGMToggle", togglePrefab, mutedIcon);
        Place(bgmToggle.transform, IconX, BgmRowY, IconSize);

        // 효과음 줄
        Place(sfxSlider.transform, SliderX, SfxRowY, SliderSize);
        TextMeshProUGUI sfxName = GetOrCreateLabel(so, "sfxLabelText", root, "SFXLabel", fontSource, "효과음");
        TextMeshProUGUI sfxValue = GetOrCreateLabel(so, "sfxValueText", root, "SFXValue", fontSource, "100%");
        PlaceName(sfxName, SfxRowY);
        PlaceValue(sfxValue, SfxRowY);
        Toggle sfxToggle = GetOrCreateToggle(so, "sfxToggle", root, "SFXToggle", togglePrefab, mutedIcon);
        Place(sfxToggle.transform, IconX, SfxRowY, IconSize);

        // 밝기 줄 (아이콘 없음)
        Place(brightnessSlider.transform, SliderX, BrightnessRowY, SliderSize);
        TextMeshProUGUI brightnessName = GetOrCreateLabel(so, "brightnessLabelText", root, "BrightnessLabel", fontSource, "밝기");
        TextMeshProUGUI brightnessValue = GetOrCreateLabel(so, "brightnessValueText", root, "BrightnessValue", fontSource, "100%");
        PlaceName(brightnessName, BrightnessRowY);
        PlaceValue(brightnessValue, BrightnessRowY);

        so.FindProperty("bgmSlider").objectReferenceValue = bgmSlider;
        so.FindProperty("bgmLabelText").objectReferenceValue = bgmName;
        so.FindProperty("bgmValueText").objectReferenceValue = bgmValue;
        so.FindProperty("bgmToggle").objectReferenceValue = bgmToggle;
        so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
        so.FindProperty("sfxLabelText").objectReferenceValue = sfxName;
        so.FindProperty("sfxValueText").objectReferenceValue = sfxValue;
        so.FindProperty("sfxToggle").objectReferenceValue = sfxToggle;
        so.FindProperty("brightnessLabelText").objectReferenceValue = brightnessName;
        so.FindProperty("brightnessValueText").objectReferenceValue = brightnessValue;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
        return true;
    }

    // SettingsManager 필드에 연결된 것을 우선, 비어 있으면 패널 자식에서 이름으로 찾는다
    private static T GetRef<T>(SerializedObject so, string field, Transform root, params string[] childNames) where T : Component
    {
        T value = field != null ? so.FindProperty(field).objectReferenceValue as T : null;
        if (value != null) return value;

        foreach (string childName in childNames)
        {
            Transform child = root.Find(childName);
            if (child != null && child.TryGetComponent(out T found)) return found;
        }
        return null;
    }

    private static void Place(Transform target, float x, float y, Vector2 size)
    {
        RectTransform rt = (RectTransform)target;
        Undo.RecordObject(rt, UndoName);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void PlaceName(TextMeshProUGUI text, float y)
    {
        Place(text.transform, NameX, y, NameSize);
        Undo.RecordObject(text, UndoName);
        text.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static void PlaceValue(TextMeshProUGUI text, float y)
    {
        Place(text.transform, ValueX, y, ValueSize);
        Undo.RecordObject(text, UndoName);
        text.alignment = TextAlignmentOptions.MidlineRight;
    }

    private static TextMeshProUGUI GetOrCreateLabel(SerializedObject so, string field, Transform root, string name,
        TextMeshProUGUI fontSource, string initialText)
    {
        TextMeshProUGUI text = GetRef<TextMeshProUGUI>(so, field, root, name);
        if (text == null)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, UndoName);
            obj.layer = root.gameObject.layer;
            obj.transform.SetParent(root, false);

            text = obj.AddComponent<TextMeshProUGUI>();
            if (fontSource != null)
            {
                text.font = fontSource.font;
                text.color = fontSource.color;
            }
            text.fontSize = LabelFontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }

        // 실행 중에는 SettingsManager가 채우지만, 에디터에서도 알아볼 수 있게 미리 넣어둔다
        Undo.RecordObject(text, UndoName);
        text.text = initialText;
        return text;
    }

    private static Toggle GetOrCreateToggle(SerializedObject so, string field, Transform root, string name,
        GameObject prefab, Sprite mutedIcon)
    {
        Toggle toggle = GetRef<Toggle>(so, field, root, name);
        if (toggle != null) return toggle;

        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        Undo.RegisterCreatedObjectUndo(obj, UndoName);
        obj.name = name;

        // 꺼져 있을 때 보이는 바탕 = 음소거 스피커, 켜지면 Active Image(빨간 스피커)가 덮는다
        Image background = obj.GetComponent<Image>();
        if (background != null) background.sprite = mutedIcon;

        toggle = obj.GetComponent<Toggle>();
        toggle.isOn = true;
        return toggle;
    }

    private static T FindInScene<T>() where T : Object
    {
        T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return found.Length > 0 ? found[0] : null;
    }
}
