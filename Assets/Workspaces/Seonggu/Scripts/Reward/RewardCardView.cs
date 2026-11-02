using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 전투 보상 화면의 카드 한 장. 마우스를 올리면 살짝 커지고, 누르면 BattleRewardManager에 선택을 알린다.
public class RewardCardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Vector3 targetScale = Vector3.one;

    private ICard cardData;
    private Action<ICard> onCardSelectedCallback;
    private void Update()
    {
        // 목표 크기로 부드럽게
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);
    }

    // 카드 내용 표시 + 눌렀을 때 알릴 함수 연결
    public void Setup(ICard card, Action<ICard> onSelected)
    {
        cardData = card;
        onCardSelectedCallback = onSelected;

        if (nameText != null) nameText.text = card.Name;
        if (costText != null) costText.text = card.Cost.ToString();
        if (descriptionText != null) descriptionText.text = CardText.Describe(card);

        // 카드 프레임 / 아이콘
        CardVisual cardVisual = GetComponent<CardVisual>();
        if (cardVisual != null) cardVisual.Apply(card);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"<color=yellow>카드 클릭됨!</color> 선택된 카드: {cardData.Name}");

        // 어떤 카드를 골랐는지 매니저에 알림
        onCardSelectedCallback?.Invoke(cardData);
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