using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    public Button button;

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