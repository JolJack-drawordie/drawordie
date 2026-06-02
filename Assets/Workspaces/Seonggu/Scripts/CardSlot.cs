using UnityEngine;

public enum SlotType { Adjective, Gerund }

[RequireComponent(typeof(BoxCollider2D))]
public class CardSlot : MonoBehaviour
{
    public SlotType slotType;
    public bool isOccupied = false;
    public GameObject currentCard = null;
    public int currentCardID;

    private void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(200, 300);
    }

    public int GetCardID() { return currentCard == null ? -1 : currentCardID; }

    public bool CanAcceptCard(CardUI card)
    {
        if (isOccupied) return false;
        if (slotType == SlotType.Adjective && card.cardType == CardType.Adjective) return true;
        if (slotType == SlotType.Gerund && card.cardType == CardType.Gerund)
            return ComboManager.Instance.adjSlot.isOccupied;
        return false;
    }

    public void PlaceCard(GameObject card)
    {
        CardUI cardUI = card.GetComponent<CardUI>();
        currentCard = card;
        currentCardID = cardUI.CardID;

        isOccupied = true;
        cardUI.isInSlot = true;

        RectTransform cardRect = card.GetComponent<RectTransform>();
        card.transform.SetParent(transform, false);
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;

        CardDraggable draggable = card.GetComponent<CardDraggable>();
        if (draggable != null) draggable.currentSlot = this;

        DataManager.Instance.RearrangeHand();
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
