using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 맵의 노드 하나 (일반 전투 / 중간보스 / 휴식 / 보스). 누르면 MapNodeManager가 해당 씬으로 이동시킨다.
public class MapNode : MonoBehaviour
{
    public enum NodeType
    {
        Monster,
        Elite,
        Rest,
        Boss
    }

    [Header("Node Info")]
    public NodeType nodeType;

    public int floor; // 층 (0부터)

    public int index; // 같은 층 안에서의 순서

    // 이 노드 전용 시드 (전투에 나올 몬스터 등을 정함, MapGenerator가 만듦)

    [Header("Node Seed")]
    public int nodeSeed;

    // 다음 층에서 이어지는 노드
    [Header("Connected Nodes")]
    public List<MapNode> connectedNodes =
        new List<MapNode>();

    [Header("UI")]
    public Button button;

    private MapNodeManager manager;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        // 씬의 노드 매니저 (클릭 처리)
        manager =
            FindFirstObjectByType<MapNodeManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnClickNode);
        }
    }

    // MapGenerator가 노드를 만들 때 종류 / 위치 / 시드 지정
    public void Initialize(
        NodeType type,
        int floorIndex,
        int nodeIndex,
        int seed)
    {
        nodeType = type;
        floor = floorIndex;
        index = nodeIndex;

        nodeSeed = seed;

        Debug.Log(
            $"Node Initialized : " +
            $"Floor {floor} / " +
            $"Index {index} / " +
            $"Type {nodeType} / " +
            $"Node Seed {nodeSeed}"
        );
    }

    // 다음 층 노드와 연결 (중복 연결은 무시)
    public void AddConnection(MapNode nextNode)
    {
        if (nextNode == null)
        {
            return;
        }

        if (!connectedNodes.Contains(nextNode))
        {
            connectedNodes.Add(nextNode);

            Debug.Log(
                $"{name} -> {nextNode.name}"
            );
        }
    }

    void OnClickNode()
    {
        Debug.Log(
            $"Select Node : " +
            $"Floor {floor} / " +
            $"Index {index} / " +
            $"Type {nodeType} / " +
            $"Node Seed {nodeSeed}"
        );

        if (manager != null)
        {
            manager.NodeSelected(this);
        }
    }

    // 지금 갈 수 있는 노드만 누를 수 있게
    public void SetInteractable(bool value)
    {
        if (button != null)
        {
            button.interactable = value;
        }
    }
}