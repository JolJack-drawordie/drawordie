using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PreviewCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void Setup(ICard card)
    {
        // UI 텍스트 갱신 (네 카드 데이터 구조에 맞춰서 수정 가능)  
        if (nameText != null) nameText.text = card.Name;
        if (costText != null) costText.text = card.Cost.ToString();
        if (descriptionText != null) descriptionText.text = card.Description;
    }
}
