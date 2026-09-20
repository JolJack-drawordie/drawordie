using UnityEngine;
using TMPro;

public class ActUIManager : MonoBehaviour
{
    [Header("Act 제목")]
    public TextMeshProUGUI actText;

    private void Start()
    {
        UpdateActText();
    }

    private void UpdateActText()
    {
        if (actText == null)
        {
            Debug.LogError("ActUIManager : Act Text가 연결되지 않았습니다.");
            return;
        }

        actText.text = "Act " + GameFlowData.currentAct;

        Debug.Log(
            "현재 Act UI 갱신 : Act " +
            GameFlowData.currentAct
        );
    }
}