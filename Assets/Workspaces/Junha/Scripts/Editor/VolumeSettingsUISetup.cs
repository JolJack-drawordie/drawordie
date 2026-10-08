using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 설정 패널(SettingsPanel)을 배경음 / 효과음 / 밝기 3줄로 배치하고 SettingsManager에 연결하는 에디터 툴.
// 맨 위에 "설정" 제목, 각 줄: [이름 라벨] [슬라이더] [% 라벨] [소리 아이콘 토글]   (밝기 줄은 아이콘 없음)
// - 기존 SoundSlider는 배경음 슬라이더로 쓰고, 복제해서 효과음 슬라이더를 만든다.
// - 아이콘은 Bloodlines UI의 Icon-Toggle 1 프리팹. 꺼짐 = 회색 음소거 스피커(Icons-2), 켜짐 = 빨간 스피커.
// - 맨 아래 닫기 / 로비로(로비 씬 제외) / 게임 종료 버튼은 Bloodlines UI의 Button 1 (Red) 프리팹으로 나란히 놓는다.
//   글꼴은 온글잎 콘콘체, 효과음은 랭킹 버튼과 같은 방식(호버: EventTrigger / 클릭: onClick → ButtonHoverSound.PlaySafely).
// - 게임 종료 / 로비로 전에 띄우는 확인 창(ConfirmDialog, 예 / 아니오 / 취소)을 설정 창 안에 만들어 둔다.
// - 이미 만들어진 요소는 다시 만들지 않고 위치만 맞춘다 (여러 번 실행해도 됨).
// 사용법: Tools > Settings > 설정 창 UI 배치 (로비/휴식/전투 씬) 실행. 세 씬을 차례로 열어 배치하고 저장한다.
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

    private const string RedButtonPrefabPath =
        "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Prefabs/Button/Button 1 (Red).prefab";
    private const string ButtonFontPath = "Assets/Workspaces/Junha/Resources/온글잎 콘콘체 SDF.asset";
    private const string HoverClipPath = "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Audio/Hover Button SFX.wav";
    private const string ClickClipPath = "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Audio/Click Button SFX.wav";

    // 패널 크기 (중앙 앵커). 예전 800x500에서 가로로 넓힘
    private static readonly Vector2 PanelSize = new Vector2(1100f, 600f);

    private const float TitleY = 235f;
    private static readonly Vector2 TitleSize = new Vector2(500f, 70f);
    private const float TitleFontSize = 52f;

    private const float BgmRowY = 150f;
    private const float SfxRowY = 70f;
    private const float BrightnessRowY = -10f;

    private const float NameX = -410f;
    private const float SliderX = -10f;
    private const float ValueX = 360f;
    private const float IconX = 455f;
    private static readonly Vector2 NameSize = new Vector2(160f, 50f);
    private static readonly Vector2 SliderSize = new Vector2(620f, 36f);
    private static readonly Vector2 ValueSize = new Vector2(90f, 50f);
    private static readonly Vector2 IconSize = new Vector2(60f, 60f);
    private const float LabelFontSize = 32f;

    // 맨 아래 버튼 줄 (가로로 나란히). 로비: [닫기] [게임 종료], 그 외: [닫기] [로비로] [게임 종료]
    private const float ButtonRowY = -190f;
    private const float TwoButtonsGap = 150f;
    private const float ThreeButtonsGap = 300f;
    private static readonly Vector2 ButtonSize = new Vector2(260f, 70f);
    private const float ButtonFontSize = 36f;

    // 확인 창 (게임 종료 / 로비로 전에 표시)
    private static readonly Color ConfirmDimColor = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Vector2 ConfirmBoxSize = new Vector2(760f, 320f);
    private const float ConfirmMessageY = 45f;
    private static readonly Vector2 ConfirmMessageSize = new Vector2(680f, 140f);
    private const float ConfirmFontSize = 40f;
    private const float ConfirmButtonY = -90f;
    private const float ConfirmButtonSpacing = 240f; // 버튼 중심 사이 간격 ([예] [아니오] [취소] 3개까지)
    private static readonly Vector2 ConfirmButtonSize = new Vector2(210f, 70f);

    private const string LobbySceneName = "LobbyScene";

    private const string UndoName = "설정 창 UI 배치";

    [MenuItem("Tools/Settings/설정 창 UI 배치 (로비, 휴식, 전투 씬)")]
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
        Place(root, 0f, 0f, PanelSize);

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

        // 닫기 / (로비로) / 게임 종료 버튼 + 확인 창
        if (!SetupBottomButtons(so, settings, root, sceneName)) return false;

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

    private static bool SetupBottomButtons(SerializedObject so, SettingsManager settings, Transform root, string sceneName)
    {
        GameObject redPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RedButtonPrefabPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ButtonFontPath);
        AudioClip hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(HoverClipPath);
        AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickClipPath);
        if (redPrefab == null || font == null || hoverClip == null || clickClip == null)
        {
            Debug.LogError("[VolumeSettingsUISetup] Red 버튼 프리팹 / 온글잎 폰트 / 버튼 효과음 중 찾을 수 없는 것이 있습니다.");
            return false;
        }

        RedButtonAssets red = new RedButtonAssets { prefab = redPrefab, font = font, hoverClip = hoverClip, clickClip = clickClip };

        Button close = GetOrCreateRedButton(root, "CloseButton", "닫기", settings.CloseSettings, red);
        Button exit = GetOrCreateRedButton(root, "ExitButton", "게임 종료", settings.OnClickExit, red);

        // 로비 씬의 설정 창에는 로비로 버튼이 필요 없음
        Button lobby = sceneName == LobbySceneName
            ? null
            : GetOrCreateRedButton(root, "LobbyButton", "로비로", settings.OnClickLobby, red);

        if (lobby == null)
        {
            PlaceButton(close, -TwoButtonsGap);
            PlaceButton(exit, TwoButtonsGap);
        }
        else
        {
            lobby.transform.SetSiblingIndex(close.transform.GetSiblingIndex() + 1);
            PlaceButton(close, -ThreeButtonsGap);
            PlaceButton(lobby, 0f);
            PlaceButton(exit, ThreeButtonsGap);
        }

        so.FindProperty("lobbyButton").objectReferenceValue = lobby;
        so.FindProperty("confirmDialog").objectReferenceValue = SetupConfirmDialog(root, red);
        return true;
    }

    private static void PlaceButton(Button button, float x)
    {
        Place(button.transform, x, ButtonRowY, ButtonSize);
        PrefabUtility.RecordPrefabInstancePropertyModifications(button.transform);
    }

    // 설정 창 전체를 어둡게 덮고 가운데에 [메시지] [예] [아니오]를 띄우는 확인 창. 평소에는 꺼둔다.
    private static ConfirmDialog SetupConfirmDialog(Transform root, RedButtonAssets red)
    {
        RectTransform dialogRect = GetOrCreateChild(root, "ConfirmDialog");
        GameObject dialogObj = dialogRect.gameObject;
        dialogRect.anchorMin = Vector2.zero;
        dialogRect.anchorMax = Vector2.one;
        dialogRect.offsetMin = Vector2.zero;
        dialogRect.offsetMax = Vector2.zero;
        dialogRect.SetAsLastSibling();

        // 뒤의 설정 창 버튼이 눌리지 않도록 막는 어두운 배경
        Image dim = GetOrAdd<Image>(dialogObj);
        dim.color = ConfirmDimColor;
        dim.raycastTarget = true;

        // 확인 창 테두리는 설정 창과 같은 그림
        RectTransform box = GetOrCreateChild(dialogRect, "Box");
        Place(box, 0f, 0f, ConfirmBoxSize);
        Image boxImage = GetOrAdd<Image>(box.gameObject);
        Image panelImage = root.GetComponent<Image>();
        if (panelImage != null) boxImage.sprite = panelImage.sprite;

        RectTransform messageRect = GetOrCreateChild(box, "Message");
        Place(messageRect, 0f, ConfirmMessageY, ConfirmMessageSize);
        TextMeshProUGUI message = GetOrAdd<TextMeshProUGUI>(messageRect.gameObject);
        message.font = red.font;
        message.fontSize = ConfirmFontSize;
        message.color = Color.white;
        message.alignment = TextAlignmentOptions.Center;
        message.raycastTarget = false;
        message.text = "정말 게임을 종료하시겠습니까?"; // 실행 중에는 ConfirmDialog.Show가 바꿈

        // 가로 위치는 실행 중 ConfirmDialog가 보이는 버튼 수(2 / 3)에 맞춰 다시 잡음. 에디터에서는 3개 기준으로 배치
        Button yes = GetOrCreateRedButton(box, "YesButton", "예", null, red);
        Button no = GetOrCreateRedButton(box, "NoButton", "아니오", null, red);
        Button cancel = GetOrCreateRedButton(box, "CancelButton", "취소", null, red);
        Button[] dialogButtons = { yes, no, cancel };
        for (int i = 0; i < dialogButtons.Length; i++)
        {
            Place(dialogButtons[i].transform, (i - 1) * ConfirmButtonSpacing, ConfirmButtonY, ConfirmButtonSize);
            PrefabUtility.RecordPrefabInstancePropertyModifications(dialogButtons[i].transform);
        }

        ConfirmDialog dialog = GetOrAdd<ConfirmDialog>(dialogObj);
        SerializedObject dialogSo = new SerializedObject(dialog);
        dialogSo.FindProperty("messageText").objectReferenceValue = message;
        dialogSo.FindProperty("yesButton").objectReferenceValue = yes;
        dialogSo.FindProperty("noButton").objectReferenceValue = no;
        dialogSo.FindProperty("cancelButton").objectReferenceValue = cancel;
        dialogSo.FindProperty("buttonSpacing").floatValue = ConfirmButtonSpacing;
        dialogSo.ApplyModifiedProperties();

        dialogObj.SetActive(false);
        return dialog;
    }

    private static RectTransform GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, UndoName);
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            child = obj.transform;
        }

        Undo.RecordObject(child, UndoName);
        Undo.RecordObject(child.gameObject, UndoName);
        return (RectTransform)child;
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null) return Undo.AddComponent<T>(obj);

        Undo.RecordObject(component, UndoName);
        return component;
    }

    private struct RedButtonAssets
    {
        public GameObject prefab;
        public TMP_FontAsset font;
        public AudioClip hoverClip;
        public AudioClip clickClip;
    }

    // name 버튼을 Red 프리팹 인스턴스로 만든다. 이미 Red 프리팹이면 그대로 돌려주고, 예전 버튼이 있으면 같은 자리에서 교체한다.
    // onClick이 null이면 효과음만 연결 (확인 창의 예 / 아니오처럼 코드에서 리스너를 붙이는 경우)
    private static Button GetOrCreateRedButton(Transform root, string name, string label, UnityAction onClick, RedButtonAssets red)
    {
        Transform old = root.Find(name);
        if (old != null && PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject) == red.prefab)
            return old.GetComponent<Button>();

        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(red.prefab, root);
        Undo.RegisterCreatedObjectUndo(obj, UndoName);
        obj.name = name;
        if (old != null) obj.transform.SetSiblingIndex(old.GetSiblingIndex());

        TMP_Text text = obj.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            text.font = red.font;
            text.fontSize = ButtonFontSize;
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }

        // 프리팹 기본 효과음(ButtonSFX)은 게임 SoundManager가 아닌 에셋 전용 SoundManager를 쓰므로 끈다 (중복 재생 방지)
        BloodlinesUI.ButtonSFX assetSfx = obj.GetComponent<BloodlinesUI.ButtonSFX>();
        if (assetSfx != null)
        {
            assetSfx.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(assetSfx);
        }

        // 랭킹 버튼과 같은 방식: 효과음 설정을 따르는 ButtonHoverSound.PlaySafely
        AudioSource source = Undo.AddComponent<AudioSource>(obj);
        source.playOnAwake = false;
        ButtonHoverSound safePlayer = Undo.AddComponent<ButtonHoverSound>(obj);

        EventTrigger trigger = Undo.AddComponent<EventTrigger>(obj);
        EventTrigger.Entry hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        UnityEventTools.AddObjectPersistentListener(hover.callback, safePlayer.PlaySafely, red.hoverClip);
        trigger.triggers.Add(hover);

        // 클릭음을 먼저 재생한 뒤 패널을 닫도록 순서 유지
        Button button = obj.GetComponent<Button>();
        UnityEventTools.AddObjectPersistentListener(button.onClick, safePlayer.PlaySafely, red.clickClip);
        if (onClick != null) UnityEventTools.AddPersistentListener(button.onClick, onClick);
        PrefabUtility.RecordPrefabInstancePropertyModifications(button);

        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        return button;
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
        text.fontSize = LabelFontSize;
    }

    private static void PlaceValue(TextMeshProUGUI text, float y)
    {
        Place(text.transform, ValueX, y, ValueSize);
        Undo.RecordObject(text, UndoName);
        text.alignment = TextAlignmentOptions.MidlineRight;
        text.fontSize = LabelFontSize;
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
