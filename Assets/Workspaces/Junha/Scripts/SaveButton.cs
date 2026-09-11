using UnityEngine;
using UnityEngine.UI;

public class SaveButton : MonoBehaviour
{
    public Button button;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClickSave);
        }
    }

    private void OnClickSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }
        else
        {
            Debug.LogWarning("[SaveButton] SaveManager를 찾을 수 없습니다.");
        }
    }
}
