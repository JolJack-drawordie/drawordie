using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 상단 바 왼쪽의 진행 정보 표시 (예: "Act 1 · 3층", 보스방은 "Act 1 · 보스", 중간보스방은 "Act 1 · 중간보스",
// 휴식 씬은 "Act 1 · 3층 휴식공간", 맵 씬은 "Act 1", 월드맵 씬은 "월드 맵")
[RequireComponent(typeof(TMP_Text))]
public class ProgressInfoText : MonoBehaviour
{
    private const string MapSceneName = "MapScene";
    private const string WorldMapSceneName = "WorldMapScene";

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label == null) return;

        string act = $"Act {GameFlowData.currentAct}";

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == WorldMapSceneName)
        {
            label.text = "월드 맵";
            return;
        }

        // 맵 씬에서는 다음에 갈 층과 헷갈리지 않도록 Act만 표시
        if (sceneName == MapSceneName)
        {
            label.text = act;
            return;
        }

        if (GameFlowData.currentNodeType == MapNode.NodeType.Boss)
        {
            label.text = $"{act} · 보스";
            return;
        }

        if (GameFlowData.currentNodeType == MapNode.NodeType.Elite)
        {
            label.text = $"{act} · 중간보스";
            return;
        }

        // currentFloor는 0부터 시작 (맵에서 노드를 고르기 전이면 -1)
        int floor = GameFlowData.currentFloor + 1;
        if (floor <= 0)
        {
            label.text = act;
            return;
        }

        label.text = GameFlowData.currentNodeType == MapNode.NodeType.Rest
            ? $"{act} · {floor}층 휴식공간"
            : $"{act} · {floor}층";
    }
}
