using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 로비 NewGame / LoadGame 버튼을 랭킹 버튼(Ranking_Btn)과 같은 모양으로 교체하는 1회성 에디터 툴.
// 랭킹 버튼을 복제하므로 프리팹(Button 1 (Gray)), 폰트, 효과음(호버: EventTrigger / 클릭: onClick → ButtonHoverSound.PlaySafely)이 그대로 따라온다.
// 기존 버튼의 위치/순서는 유지하고, LobbyManager의 newGameButton / lordGameButton 참조를 새 버튼으로 바꾼 뒤 기존 버튼은 삭제한다.
// 사용법: LobbyScene을 연 상태에서 Tools > Lobby > NewGame·LoadGame 버튼을 랭킹 버튼 스타일로 교체 실행 후 씬 저장.
public static class LobbyMainButtonsRestyle
{
    [MenuItem("Tools/Lobby/NewGame·LoadGame 버튼을 랭킹 버튼 스타일로 교체")]
    private static void Restyle()
    {
        LobbyManager lobbyManager = Object.FindFirstObjectByType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("[LobbyMainButtonsRestyle] 씬에서 LobbyManager를 찾을 수 없습니다. LobbyScene을 열어주세요.");
            return;
        }

        SerializedObject so = new SerializedObject(lobbyManager);
        Button rankingButton = so.FindProperty("rankingButton").objectReferenceValue as Button;
        if (rankingButton == null)
        {
            Debug.LogError("[LobbyMainButtonsRestyle] LobbyManager의 rankingButton 참조가 비어 있습니다. 복제할 랭킹 버튼이 필요합니다.");
            return;
        }

        Undo.SetCurrentGroupName("로비 버튼 랭킹 스타일로 교체");
        int undoGroup = Undo.GetCurrentGroup();

        ReplaceButton(so.FindProperty("newGameButton"), rankingButton, "NewGame_Btn", "새 게임");
        ReplaceButton(so.FindProperty("lordGameButton"), rankingButton, "LoadGame_Btn", "불러오기");

        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(lobbyManager.gameObject.scene);
        Selection.activeGameObject = null;

        Debug.Log("<color=green>[LobbyMainButtonsRestyle] NewGame / LoadGame 버튼 교체 완료! 씬을 저장하세요.</color>");
    }

    private static void ReplaceButton(SerializedProperty buttonProp, Button template, string name, string label)
    {
        Button oldButton = buttonProp.objectReferenceValue as Button;
        if (oldButton == null)
        {
            Debug.LogError($"[LobbyMainButtonsRestyle] LobbyManager의 {buttonProp.name} 참조가 비어 있습니다.");
            return;
        }

        // 이미 교체된 버튼(효과음 컴포넌트가 붙어 있음)이면 건너뜀
        if (oldButton.GetComponent<ButtonHoverSound>() != null)
        {
            Debug.LogWarning($"[LobbyMainButtonsRestyle] '{oldButton.name}'은 이미 랭킹 버튼 스타일입니다. 건너뜁니다.");
            return;
        }

        // Ctrl+D와 같은 방식으로 복제 → 프리팹 연결 유지 + 효과음 리스너가 복제본의 ButtonHoverSound를 가리키도록 재연결됨
        Selection.activeGameObject = template.gameObject;
        Unsupported.DuplicateGameObjectsUsingPasteboard();
        GameObject copy = Selection.activeGameObject;
        if (copy == null || copy == template.gameObject)
        {
            Debug.LogError("[LobbyMainButtonsRestyle] 랭킹 버튼 복제에 실패했습니다.");
            return;
        }

        RectTransform oldRect = (RectTransform)oldButton.transform;
        RectTransform rect = (RectTransform)copy.transform;

        Undo.RecordObject(copy, "버튼 이름 변경");
        copy.name = name;

        Undo.RecordObject(rect, "버튼 위치 변경");
        rect.anchorMin = oldRect.anchorMin;
        rect.anchorMax = oldRect.anchorMax;
        rect.pivot = oldRect.pivot;
        rect.anchoredPosition = oldRect.anchoredPosition;
        rect.sizeDelta = oldRect.sizeDelta;
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);

        TMP_Text text = copy.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            Undo.RecordObject(text, "버튼 글자 변경");
            text.text = label;
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }

        int siblingIndex = oldRect.GetSiblingIndex();
        Undo.DestroyObjectImmediate(oldButton.gameObject);
        rect.SetSiblingIndex(siblingIndex);

        // 클릭 이벤트(OnNewGameClick / OnLordGameClick)는 LobbyManager.Start에서 코드로 연결됨
        buttonProp.objectReferenceValue = copy.GetComponent<Button>();
    }
}
