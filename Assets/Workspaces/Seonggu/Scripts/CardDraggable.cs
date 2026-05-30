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
        }

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false; 
        transform.SetAsLastSibling();

        if (cardUI.cardType == CardType.Adjective)
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

        // 🚀 핵심: 카드를 놓는 순간, 카드의 정중앙 좌표 아래에 있는 모든 물리 콜라이더를 뚫어봅니다!
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        foreach (Collider2D hit in hits)
        {
            // 1. 적(EnemyTarget) 컴포넌트를 맞췄다면?
            EnemyTarget enemy = hit.GetComponent<EnemyTarget>();
            if (enemy != null && (cardUI.cardType == CardType.Gerund || cardUI.cardType == CardType.Synergy))
            {
                enemy.ReceiveCard(cardUI);
                return;
            }

            // 2. 슬롯(CardSlot) 컴포넌트를 맞췄다면?
            CardSlot slot = hit.GetComponent<CardSlot>();
            if (slot != null && slot.CanAcceptCard(cardUI))
            {
                slot.PlaceCard(gameObject);
                return;
            }
        }

        // 아무것도 못 맞췄다면 다시 패로 튕겨냅니다.
        ComboManager.Instance.HideSlots();
        transform.SetParent(DataManager.Instance.handArea);
        DataManager.Instance.RearrangeHand(); 
    }
}