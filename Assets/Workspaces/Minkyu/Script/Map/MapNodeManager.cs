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
        // 배경음악
        if (SoundManager.Instance != null && SoundManager.Instance.mapBackgroundSound != null) {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.mapBackgroundSound);
        }
    }

    public void GoToBattle()
    {
        SceneManager.LoadScene(battleSceneName);
        // 배경 음악 정지
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
        }
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