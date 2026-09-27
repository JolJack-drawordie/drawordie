using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 게임 클리어 화면(ResultPanel)에 로비와 같은 랭킹 버튼을 배치하고 UIManager.clearRankingButton에 연결하는 1회성 에디터 툴.
// 로비 Ranking_Btn과 동일: Button 1 (Gray) 프리팹, "랭킹", 온글잎 콘콘체 40, 300x65, 호버/클릭 사운드
// 사용법: 전투 씬(junhaTest)을 연 상태에서 Tools > Ranking > 클리어 화면 랭킹 버튼 배치 실행 후 씬 저장.
public static class ClearRankingButtonSetup
{
    private const string ButtonPrefabPath =
        "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Prefabs/Button/Button 1 (Gray).prefab";
    private const string ClickSoundPath =
        "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Audio/Click Button SFX.wav";
    private const string HoverSoundPath =
        "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Audio/Hover Button SFX.wav";
    private const string FontGuid = "87b7a5c12adc07c4fa07bec4ec66bf03"; // 온글잎 콘콘체 SDF (로비 랭킹 버튼과 동일)

    // Next 버튼(0, -77) 아래
    private static readonly Vector2 ButtonPosition = new Vector2(0f, -150f);
    private static readonly Vector2 ButtonSize = new Vector2(300f, 65f);

    [MenuItem("Tools/Ranking/클리어 화면 랭킹 버튼 배치")]
    private static void Setup()
    {
        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("[ClearRankingButtonSetup] 씬에서 UIManager를 찾을 수 없습니다. 전투 씬(junhaTest)을 열어주세요.");
            return;
        }

        if (uiManager.clearRankingButton != null)
        {
            Debug.LogWarning("[ClearRankingButtonSetup] 이미 clearRankingButton이 연결되어 있습니다. 다시 배치하려면 기존 버튼을 지우고 연결을 비워주세요.");
            return;
        }

        if (uiManager.resultPanel == null)
        {
            Debug.LogError("[ClearRankingButtonSetup] UIManager의 resultPanel 참조가 비어 있습니다.");
            return;
        }

        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null)
        {
            Debug.LogError($"[ClearRankingButtonSetup] 버튼 프리팹을 찾을 수 없습니다: {ButtonPrefabPath}");
            return;
        }

        AudioClip clickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(ClickSoundPath);
        AudioClip hoverSound = AssetDatabase.LoadAssetAtPath<AudioClip>(HoverSoundPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));

        Undo.SetCurrentGroupName("클리어 화면 랭킹 버튼 배치");
        int undoGroup = Undo.GetCurrentGroup();

        // 버튼 생성
        GameObject buttonObj = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, uiManager.resultPanel.transform);
        Undo.RegisterCreatedObjectUndo(buttonObj, "랭킹 버튼 생성");
        buttonObj.name = "Ranking_Btn";

        RectTransform rect = (RectTransform)buttonObj.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = ButtonPosition;
        rect.sizeDelta = ButtonSize;

        TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "랭킹";
            label.fontSize = 40f;
            if (font != null)
            {
                label.font = font;
                label.fontSharedMaterial = font.material;
            }
        }

        // 사운드: 씬 전환/팝업에도 끊기지 않도록 ButtonHoverSound.PlaySafely 사용 (전역 SoundManager 우선)
        AudioSource audioSource = Undo.AddComponent<AudioSource>(buttonObj);
        audioSource.playOnAwake = false;
        ButtonHoverSound safePlayer = Undo.AddComponent<ButtonHoverSound>(buttonObj);

        Button button = buttonObj.GetComponent<Button>();
        if (clickSound != null)
            UnityEventTools.AddObjectPersistentListener(button.onClick, safePlayer.PlaySafely, clickSound);

        if (hoverSound != null)
        {
            EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
            if (trigger == null) trigger = Undo.AddComponent<EventTrigger>(buttonObj);

            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            UnityEventTools.AddObjectPersistentListener(entry.callback, safePlayer.PlaySafely, hoverSound);
            trigger.triggers.Add(entry);
        }

        // UIManager 연결 (클릭 시 랭킹 열기는 UIManager.Awake에서 코드로 연결, 평소에는 숨김)
        Undo.RecordObject(uiManager, "clearRankingButton 연결");
        uiManager.clearRankingButton = button;

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(uiManager.gameObject.scene);
        Selection.activeGameObject = buttonObj;

        Debug.Log("<color=green>[ClearRankingButtonSetup] 클리어 화면 랭킹 버튼 배치 완료! 씬을 저장하세요.</color>");
    }
}
