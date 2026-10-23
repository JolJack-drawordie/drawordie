using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 상단 바 왼쪽의 진행 정보 표시 (예: "Act 1 · 3층", 보스방은 "Act 1 · 보스", 중간보스방은 "Act 1 · 중간보스",
// 휴식 씬은 "Act 1 · 3층 휴식공간", 맵 씬은 "Act 1 - 잊혀진 숲의 유적", 월드맵 씬은 "월드 맵")
[RequireComponent(typeof(TMP_Text))]
public class ProgressInfoText : MonoBehaviour
{
    private const string MapSceneName = "MapScene";
    private const string WorldMapSceneName = "WorldMapScene";

    // 월드맵(WorldMapScene의 WorldMapManager.acts[].title)과 같은 Act 제목
    private static readonly string[] ActTitles = { "잊혀진 숲의 유적", "불타는 화산섬", "마계의 성채" };

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

        // 맵 씬에서는 다음에 갈 층과 헷갈리지 않도록 Act와 제목만 표시
        if (sceneName == MapSceneName)
        {
            int index = GameFlowData.currentAct - 1;
            label.text = index >= 0 && index < ActTitles.Length
                ? $"{act} - {ActTitles[index]}"
                : act;
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
