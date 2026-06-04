using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private CardUI cardUI;

    public CardSlot currentSlot;
    public bool isDragging = false;

    // 🚀 Awake에서 Start로 변경하여 모든 준비가 끝난 후 세팅!
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        cardUI = GetComponent<CardUI>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true; 
        box.size = new Vector2(250, 360); // 카드 크기 강제 고정!
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        cardUI.isInSlot = false;

        if (currentSlot != null)
        {
            currentSlot.RemoveCard();
            currentSlot = null;
            // 슬롯의 자식에서 handArea로 올려야 드래그 좌표계가 Canvas 기준으로 통일됨
            transform.SetParent(DataManager.Instance.handArea);
        }

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();

        if (cardUI.cardType == CardType.Adjective)
            ComboManager.Instance.ShowSlots();
        else if (cardUI.cardType == CardType.Gerund && ComboManager.Instance.adjSlot.isOccupied)
            ComboManager.Instance.ShowSlots();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 슬롯 판정: RectTransformUtility로 화면 좌표 포함 여부 직접 확인
        // → Physics2D 좌표계 혼란 없이 Canvas 스케일을 자동 처리함
        CardSlot[] slots = { ComboManager.Instance.adjSlot, ComboManager.Instance.gerSlot };
        foreach (CardSlot slot in slots)
        {
            if (!slot.gameObject.activeInHierarchy) continue;
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(slotRect, eventData.position, canvas.worldCamera)
                && slot.CanAcceptCard(cardUI))
            {
                slot.PlaceCard(gameObject);
                return;
            }
        }

        // 적 판정: 원래 동작하던 Physics2D 방식 복원
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D hit in hits)
        {
            EnemyTarget enemy = hit.GetComponent<EnemyTarget>();
            if (enemy != null && (cardUI.cardType == CardType.Gerund || cardUI.cardType == CardType.Synergy))
            {
                // 형용사 슬롯에 카드가 있으면 공격 불가 → 패로 반환
                if (ComboManager.Instance.adjSlot.isOccupied)
                {
                    transform.SetParent(DataManager.Instance.handArea);
                    DataManager.Instance.RearrangeHand();
                    return;
                }
                enemy.ReceiveCard(cardUI);
                return;
            }
        }

        // 아무것도 못 맞췄다면 다시 패로 튕겨냅니다.
        // 형용사 슬롯이 차있으면 슬롯 패널 유지
        if (!ComboManager.Instance.adjSlot.isOccupied)
            ComboManager.Instance.HideSlots();
        transform.SetParent(DataManager.Instance.handArea);
        DataManager.Instance.RearrangeHand();
    }
}