using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 덱 보기 팝업 안의 카드 한 장. 마우스를 올리면 살짝 커진다.
public class ViewerCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Vector3 targetScale = Vector3.one;

    private void Update()
    {
        // 목표 크기로 부드럽게
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);
    }

    // 카드 이름 / 비용 / 설명(수치 포함) 표시
    public void Setup(ICard card)
    {
        if (nameText != null) nameText.text = card.Name;
        if (costText != null) costText.text = card.Cost.ToString();
        if (descriptionText != null) descriptionText.text = CardText.Describe(card);

        // 카드 프레임 / 아이콘
        CardVisual cardVisual = GetComponent<CardVisual>();
        if (cardVisual != null) cardVisual.Apply(card);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * 1.15f; // 크게
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }
}