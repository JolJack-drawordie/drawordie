using UnityEngine;

public enum SlotType { Adjective, Gerund }

[RequireComponent(typeof(BoxCollider2D))]
public class CardSlot : MonoBehaviour
{
    public SlotType slotType;
    public bool isOccupied = false;
    public GameObject currentCard = null;
    public int currentCardID;

    // 🚀 Start를 사용하여 UI가 그려질 시간을 주고 넉넉한 충돌 크기를 지정!
    private void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(200, 300); // 바늘구멍 탈출! 넉넉한 타겟 사이즈
    }

    public int GetCardID() { return currentCard == null ? -1 : currentCardID; }

    public bool CanAcceptCard(CardUI card)
    {
        if (isOccupied) return false;
        if (slotType == SlotType.Adjective && card.cardType == CardType.Adjective) return true;
        if (slotType == SlotType.Gerund && card.cardType == CardType.Gerund)
        {
            if (ComboManager.Instance.adjSlot.isOccupied) return true;
        }
        return false;
    }

    public void PlaceCard(GameObject card)
    {
        CardUI cardUI = card.GetComponent<CardUI>();
        currentCard = card;
        currentCardID = cardUI.CardID;
        
        isOccupied = true;
        cardUI.isInSlot = true; 

        card.transform.SetParent(transform, true);
        card.transform.position = transform.position; // 눈에 보이는 위치 정확히 일치
        
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;

        CardDraggable draggable = card.GetComponent<CardDraggable>();
        if (draggable != null) draggable.currentSlot = this;

        ComboManager.Instance.OnSlotUpdated();
    }

    public void RemoveCard()
    {
        currentCard = null;
        currentCardID = -1;
        isOccupied = false;
        ComboManager.Instance.OnSlotUpdated();
    }
}