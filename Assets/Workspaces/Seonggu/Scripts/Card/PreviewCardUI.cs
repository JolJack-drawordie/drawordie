using TMPro;
using UnityEngine;

// 조합 슬롯에 재료 두 장이 들어오면 보여 주는 결과 카드 미리보기
public class PreviewCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;

    public void Setup(Combination combo)
    {
        // 결과 카드의 이름 / 비용 / 설명(수치 포함) 표시
        if (nameText != null) nameText.text = combo.skillName;
        if (costText != null) costText.text = combo.finalCost.ToString();
        if (descriptionText != null) descriptionText.text = CardText.Describe(combo);

        // 카드 프레임 / 아이콘
        CardVisual cardVisual = GetComponent<CardVisual>();
        if (cardVisual != null) cardVisual.Apply(combo);
    }
}
