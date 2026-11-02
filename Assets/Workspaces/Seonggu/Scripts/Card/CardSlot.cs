using UnityEngine;

// 조합 슬롯 종류: 형용사 칸 / 동명사 칸
public enum SlotType { Adjective, Gerund }

// 조합 슬롯 하나. 형용사 칸에는 형용사 카드, 동명사 칸에는 형용사 칸이 찬 뒤에만 동명사 카드를 놓을 수 있다.
[RequireComponent(typeof(BoxCollider2D))]
public class CardSlot : MonoBehaviour
{
    public SlotType slotType;
    public bool isOccupied = false;
    public GameObject currentCard = null;
    public int currentCardID; // 놓인 카드의 서버 id

    private void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(200, 300); // 카드 드롭 판정용 충돌 박스
    }

    // 놓인 카드 id (없으면 -1)
    public int GetCardID() { return currentCard == null ? -1 : currentCardID; }

    // 이 칸에 이 카드를 놓을 수 있는지 (동명사는 형용사 칸이 차 있어야 함)
    public bool CanAcceptCard(CardUI card)
    {
        if (isOccupied) return false;
        if (slotType == SlotType.Adjective && card.cardType == CardType.Adjective) return true;
        if (slotType == SlotType.Gerund && card.cardType == CardType.Gerund)
            return ComboManager.Instance.adjSlot.isOccupied;
        return false;
    }

    // 카드를 칸에 놓고 칸 가운데에 맞춘 뒤, 덱 / 손패 / 조합 미리보기를 갱신
    public void PlaceCard(GameObject card)
    {
        CardUI cardUI = card.GetComponent<CardUI>();
        currentCard = card;
        currentCardID = cardUI.CardID;

        isOccupied = true;
        cardUI.isInSlot = true;

        DeckManager.Instance.EquipCardToSlot(cardUI.cardData, slotType);

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

    // 칸 비우기
    public void RemoveCard()
    {
        currentCard = null;
        currentCardID = -1;
        isOccupied = false;
        DeckManager.Instance.ClearSlot(slotType);
        ComboManager.Instance.OnSlotUpdated();
    }
}
