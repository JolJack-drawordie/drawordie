using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeManager : MonoBehaviour
{
    [Header("Battle Scene")]
    public string battleSceneName = "JunhaTest";

    private MapNode[] allNodes;

    private void Start()
    {
        // 현재 맵의 모든 노드 찾기
        allNodes =
            FindObjectsByType<MapNode>(
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
    /// 맵 진입 시 노드 상태 초기화
    /// </summary>
    void InitializeNodes()
    {
        // 우선 모든 노드 비활성화
        foreach (MapNode node in allNodes)
        {
            node.SetInteractable(false);
        }

        // 방문했던 노드 복원
        foreach (MapNode node in allNodes)
        {
            if (GameFlowData.IsNodeVisited(node))
            {
                node.SetVisited();
            }
        }

        // 아직 아무 노드도 선택하지 않은 경우
        if (GameFlowData.currentFloor == -1)
        {
            foreach (MapNode node in allNodes)
            {
                if (node.floor == 0)
                {
                    node.SetInteractable(true);
                }
            }

            Debug.Log(
                "맵 시작 - 0층 노드 활성화"
            );

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

        // 이전 노드를 찾지 못한 경우
        if (currentNode == null)
        {
            Debug.LogWarning(
                $"이전 노드를 찾을 수 없습니다. " +
                $"Floor : {GameFlowData.currentFloor}, " +
                $"Index : {GameFlowData.currentIndex}"
            );

            return;
        }

        Debug.Log(
            $"현재 진행 위치 : " +
            $"Floor {currentNode.floor} / " +
            $"Index {currentNode.index}"
        );

        // 현재 노드는 방문한 상태
        currentNode.SetVisited();

        // 현재 노드와 연결된 다음 노드만 활성화
        foreach (MapNode nextNode in currentNode.connectedNodes)
        {
            // 이미 방문한 노드는 활성화하지 않음
            if (GameFlowData.IsNodeVisited(nextNode))
            {
                continue;
            }

            nextNode.SetInteractable(true);

            Debug.Log(
                $"다음 노드 활성화 : " +
                $"Floor {nextNode.floor} / " +
                $"Index {nextNode.index}"
            );
        }
    }

    /// <summary>
    /// 노드 클릭 시 호출
    /// </summary>
    public void NodeSelected(MapNode selectedNode)
    {
        if (selectedNode == null)
            return;

        // 선택한 노드 정보 저장
        GameFlowData.SelectNode(selectedNode);

        // 방문한 노드로 저장
        GameFlowData.AddVisitedNode(selectedNode);

        Debug.Log(
            $"노드 선택 : " +
            $"Floor {selectedNode.floor} / " +
            $"Index {selectedNode.index} / " +
            $"Type {selectedNode.nodeType}"
        );

        // 전투씬 이동
        SceneManager.LoadScene(battleSceneName);

        // 배경음악 정지
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }
    }
}