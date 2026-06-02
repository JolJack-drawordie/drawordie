using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeManager : MonoBehaviour
{
    public string battleSceneName = "JunhaTest";

    [Header("Map Nodes")]
    public MapNode[] level1Nodes;
    public MapNode[] level2Nodes;
    public MapNode[] bossNodes;

    private void Start()
    {
        UpdateNodeState();
    }

    public void GoToBattle()
    {
        SceneManager.LoadScene(battleSceneName);
    }

    private void UpdateNodeState()
    {
        SetNodes(level1Nodes, GameFlowData.clearedNodeLevel == 0);
        SetNodes(level2Nodes, GameFlowData.clearedNodeLevel == 1);
        SetNodes(bossNodes, GameFlowData.clearedNodeLevel >= 2);
    }

    private void SetNodes(MapNode[] nodes, bool interactable)
    {
        foreach (MapNode node in nodes)
        {
            if (node != null)
            {
                node.SetInteractable(interactable);
            }
        }
    }
}