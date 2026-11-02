using UnityEngine;
using UnityEngine.SceneManagement;

// 맵 씬에서 지금 갈 수 있는 노드만 누를 수 있게 하고, 노드를 고르면 전투 / 휴식 씬으로 이동한다.
public class MapNodeManager : MonoBehaviour
{
    [Header("Battle Scene")]
    public string battleSceneName = "BattleScene";

    [Header("Rest Scene")]
    public string restSceneName = "RestScene";

    private MapNode[] allNodes;

    private void Start()
    {
        // 지금 맵의 모든 노드
        allNodes = FindObjectsByType<MapNode>(
            FindObjectsSortMode.None
        );

        InitializeNodes();

        // 맵 배경음
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(
                SoundManager.Instance.GetMapBGM()
            );
        }
    }

    // 맵에 들어왔을 때 누를 수 있는 노드 결정: 처음이면 0층, 아니면 지난번 노드에서 이어지는 노드만
    void InitializeNodes()
    {
        // 일단 모든 노드를 막고
        foreach (MapNode node in allNodes)
        {
            node.SetInteractable(false);
        }

        // 처음 들어온 맵이면 0층만
        if (GameFlowData.currentFloor == -1)
        {
            // 테스트용: 보스 노드를 바로 고를 수 있게
            if (GameFlowData.debugUnlockBoss)
            {
                foreach (MapNode node in allNodes)
                {
                    if (node.nodeType == MapNode.NodeType.Boss)
                    {
                        node.SetInteractable(true);
                    }
                }

                return;
            }

            foreach (MapNode node in allNodes)
            {
                if (node.floor == 0)
                {
                    node.SetInteractable(true);
                }
            }

            return;
        }

        // 지난번에 고른 노드 찾기
        MapNode currentNode = null;

        foreach (MapNode node in allNodes)
        {
            if (node.floor == GameFlowData.currentFloor &&
                node.index == GameFlowData.currentIndex)
            {
                currentNode = node;
                break;
            }
        }

        if (currentNode == null)
        {
            return;
        }

        // 그 노드에서 이어지는 다음 노드만 누를 수 있게
        foreach (MapNode nextNode in currentNode.connectedNodes)
        {
            nextNode.SetInteractable(true);
        }
    }

    // 노드를 눌렀을 때: 위치 기록 후 휴식 노드는 휴식 씬, 나머지는 전투 씬으로
    public void NodeSelected(MapNode selectedNode)
    {
        if (selectedNode == null)
        {
            Debug.LogWarning("선택된 노드가 없습니다.");
            return;
        }

        GameFlowData.SelectNode(selectedNode);

        Debug.Log(
            $"선택한 노드 : Floor {selectedNode.floor} / " +
            $"Index {selectedNode.index} / " +
            $"Type {selectedNode.nodeType}"
        );

        // 휴식 노드 → 휴식 씬
        if (selectedNode.nodeType == MapNode.NodeType.Rest)
        {
            Debug.Log("Rest 노드 선택 → RestScene 이동");

            SceneManager.LoadScene(restSceneName);
        }
        else
        {
            // 일반 전투 / 중간보스 / 보스 → 전투 씬
            Debug.Log("전투 노드 선택 → Battle Scene 이동");

            SceneManager.LoadScene(battleSceneName);
        }

        // 맵 배경음 정지
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }
    }
}