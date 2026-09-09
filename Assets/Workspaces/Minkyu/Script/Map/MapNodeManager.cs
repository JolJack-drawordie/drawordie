using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeManager : MonoBehaviour
{
    [Header("Battle Scene")]
    public string battleSceneName = "JunhaTest";

    [Header("Rest Scene")]
    public string restSceneName = "RestScene";

    private MapNode[] allNodes;

    private void Start()
    {
        // 현재 맵의 모든 노드 찾기
        allNodes = FindObjectsByType<MapNode>(
            FindObjectsSortMode.None
        );

        InitializeNodes();

        // 배경음악
        if (SoundManager.Instance != null &&
            SoundManager.Instance.mapBackgroundSound != null)
        {
            SoundManager.Instance.PlayBGM(
                SoundManager.Instance.mapBackgroundSound
            );
        }
    }

    /// <summary>
    /// 맵 진입 시 활성화할 노드 결정
    /// </summary>
    void InitializeNodes()
    {
        // 우선 모든 노드 비활성화
        foreach (MapNode node in allNodes)
        {
            node.SetInteractable(false);
        }

        // 첫 시작이면 0층만 활성화
        if (GameFlowData.currentFloor == -1)
        {
            foreach (MapNode node in allNodes)
            {
                if (node.floor == 0)
                {
                    node.SetInteractable(true);
                }
            }

            return;
        }

        // 이전에 선택했던 노드 찾기
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

        // 현재 노드에서 연결된 다음 노드만 활성화
        foreach (MapNode nextNode in currentNode.connectedNodes)
        {
            nextNode.SetInteractable(true);
        }
    }

    /// <summary>
    /// 노드 클릭 시 호출
    /// </summary>
    public void NodeSelected(MapNode selectedNode)
    {
        if (selectedNode == null)
        {
            Debug.LogWarning("선택된 노드가 없습니다.");
            return;
        }

        // 선택한 노드 정보 저장
        GameFlowData.SelectNode(selectedNode);

        Debug.Log(
            $"선택한 노드 : Floor {selectedNode.floor} / " +
            $"Index {selectedNode.index} / " +
            $"Type {selectedNode.nodeType}"
        );

        // Rest 노드라면 RestScene으로 이동
        if (selectedNode.nodeType == MapNode.NodeType.Rest)
        {
            Debug.Log("Rest 노드 선택 → RestScene 이동");

            SceneManager.LoadScene(restSceneName);
        }
        else
        {
            // Monster / Elite / Boss는 기존 전투씬으로 이동
            Debug.Log("전투 노드 선택 → Battle Scene 이동");

            SceneManager.LoadScene(battleSceneName);
        }

        // 맵 배경음악 정지
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }
    }
}