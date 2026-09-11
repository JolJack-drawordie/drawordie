using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public int floor;

    public int index;

    // =========================
    // Node Seed
    // =========================

    [Header("Node Seed")]
    public int nodeSeed;

    [Header("Connected Nodes")]
    public List<MapNode> connectedNodes =
        new List<MapNode>();

    [Header("UI")]
    public Button button;

    [Header("Progress")]
    public bool isVisited = false;

    private MapNodeManager manager;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        // Unity 6 권장 방식
        manager =
            FindFirstObjectByType<MapNodeManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnClickNode);
        }
    }

    public void Initialize(
        NodeType type,
        int floorIndex,
        int nodeIndex,
        int seed)
    {
        nodeType = type;
        floor = floorIndex;
        index = nodeIndex;

        // 노드 전용 Seed 저장
        nodeSeed = seed;

        isVisited = false;

        Debug.Log(
            $"Node Initialized : " +
            $"Floor {floor} / " +
            $"Index {index} / " +
            $"Type {nodeType} / " +
            $"Node Seed {nodeSeed}"
        );
    }

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

    public void SetInteractable(bool value)
    {
        if (button != null)
        {
            button.interactable = value;
        }
    }

    /// <summary>
    /// 방문한 노드로 설정
    /// </summary>
    public void SetVisited()
    {
        isVisited = true;

        SetInteractable(false);

        Debug.Log(
            $"Visited Node : " +
            $"Floor {floor} / " +
            $"Index {index}"
        );
    }
}