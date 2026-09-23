using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PreviewCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;

    public void Setup(Combination combo)
    {
        // UI 텍스트 갱신 (네 카드 데이터 구조에 맞춰서 수정 가능)  
        if (nameText != null) nameText.text = combo.skillName;
        if (costText != null) costText.text = combo.finalCost.ToString();
        if (descriptionText != null) descriptionText.text = combo.description.ToString();
    }
}
