using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeManager : MonoBehaviour
{
    [Header("Battle Scene")]
    public string battleSceneName = "JunhaTest";

    [Header("Map Nodes (Legacy)")]
    public MapNode[] level1Nodes;
    public MapNode[] level2Nodes;
    public MapNode[] bossNodes;

    [Header("Runtime Nodes")]
    public List<MapNode> generatedNodes = new List<MapNode>();

    private void Start()
    {
        UpdateNodeState();

        // 배경음악
        if (SoundManager.Instance != null &&
            SoundManager.Instance.mapBackgroundSound != null)
        {
            SoundManager.Instance.PlayBGM(
                SoundManager.Instance.mapBackgroundSound);
        }
    }

    public void RegisterNode(MapNode node)
    {
        if (!generatedNodes.Contains(node))
        {
            generatedNodes.Add(node);
        }
    }

    public void ClearNodes()
    {
        generatedNodes.Clear();
    }

    public void GoToBattle()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        SceneManager.LoadScene(battleSceneName);
    }

    private void UpdateNodeState()
    {
        SetNodes(level1Nodes,
            GameFlowData.clearedNodeLevel == 0);

        SetNodes(level2Nodes,
            GameFlowData.clearedNodeLevel == 1);

        SetNodes(bossNodes,
            GameFlowData.clearedNodeLevel >= 2);
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