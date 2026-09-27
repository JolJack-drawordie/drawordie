using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 로비 LoadGame 버튼 아래에 랭킹 버튼을 배치하고 LobbyManager.rankingButton에 연결하는 1회성 에디터 툴.
// 사용법: LobbyScene을 연 상태에서 Tools > Ranking > 로비 랭킹 버튼 배치 실행 후 씬 저장.
public static class LobbyRankingButtonSetup
{
    // 로비 버튼 간격 (NewGame -50, LoadGame -200)
    private const float ButtonSpacing = 150f;

    [MenuItem("Tools/Ranking/로비 랭킹 버튼 배치")]
    private static void Setup()
    {
        LobbyManager lobbyManager = Object.FindFirstObjectByType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("[LobbyRankingButtonSetup] 씬에서 LobbyManager를 찾을 수 없습니다. LobbyScene을 열어주세요.");
            return;
        }

        SerializedObject so = new SerializedObject(lobbyManager);
        SerializedProperty rankingButtonProp = so.FindProperty("rankingButton");
        SerializedProperty loadButtonProp = so.FindProperty("lordGameButton");

        if (rankingButtonProp.objectReferenceValue != null)
        {
            Debug.LogWarning("[LobbyRankingButtonSetup] 이미 rankingButton이 연결되어 있습니다. 다시 배치하려면 기존 버튼을 지우고 연결을 비워주세요.");
            return;
        }

        Button loadButton = loadButtonProp.objectReferenceValue as Button;
        if (loadButton == null)
        {
            Debug.LogError("[LobbyRankingButtonSetup] LobbyManager의 lordGameButton 참조가 비어 있습니다.");
            return;
        }

        RectTransform loadRect = (RectTransform)loadButton.transform;

        Undo.SetCurrentGroupName("로비 랭킹 버튼 배치");
        int undoGroup = Undo.GetCurrentGroup();

        Button rankingButton = RankingUIFactory.CreateStyledButton(
            loadRect.parent, "Ranking_Btn", "RANKING", loadRect.sizeDelta, 40f);
        Undo.RegisterCreatedObjectUndo(rankingButton.gameObject, "랭킹 버튼 생성");

        RectTransform rect = (RectTransform)rankingButton.transform;
        rect.anchorMin = loadRect.anchorMin;
        rect.anchorMax = loadRect.anchorMax;
        rect.pivot = loadRect.pivot;
        rect.anchoredPosition = loadRect.anchoredPosition + new Vector2(0f, -ButtonSpacing);
        rect.SetSiblingIndex(loadRect.GetSiblingIndex() + 1);

        // 클릭 이벤트는 LobbyManager.Start에서 코드로 연결됨
        rankingButtonProp.objectReferenceValue = rankingButton;
        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(lobbyManager.gameObject.scene);
        Selection.activeGameObject = rankingButton.gameObject;

        Debug.Log("<color=green>[LobbyRankingButtonSetup] 랭킹 버튼 배치 완료! 씬을 저장하세요.</color>");
    }
}
