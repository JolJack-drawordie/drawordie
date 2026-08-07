using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [Header("UI")]
    public Button button;

    [Header("Node Info")]
    public int floor;
    public int nodeIndex;

    [Header("Connection")]
    public List<MapNode> connectedNodes = new List<MapNode>();

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
        {
            button.interactable = value;
        }
    }
}