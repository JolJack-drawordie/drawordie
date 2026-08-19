using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        // 부드럽게 커지고 위로 올라가는 연출
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);
    }

    public void Setup(ICard card, Action<ICard> onSelected)
    {
        cardData = card;
        onCardSelectedCallback = onSelected;

        // UI 텍스트 갱신 (네 카드 데이터 구조에 맞춰서 수정 가능)
        if (nameText != null) nameText.text = card.Name;
        if (costText != null) costText.text = card.Cost.ToString();
        if (descriptionText != null) descriptionText.text = card.Description;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"<color=yellow>카드 클릭됨!</color> 선택된 카드: {cardData.Name}");

        // 매니저에게 어떤 카드가 눌렸다고 알려줌
        onCardSelectedCallback?.Invoke(cardData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = Vector3.one * 1.15f; // 크기 키우기
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = Vector3.one;
    }

}