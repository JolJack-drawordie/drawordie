using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    // 드래그 중에만 붙이는 정렬용 Canvas (슬롯 패널보다 위에 그리기 위함)
    private Canvas dragCanvas;

    // 드래그 중 대상 위에 올렸을 때 카드 반응 (올바른 대상: 선명하게 / 잘못된 대상: 붉게)
    private enum HoverState { None, Valid, Invalid }
    private HoverState hoverState;
    private bool canUseOnTarget;
    private Image[] tintImages;
    private Color[] tintBaseColors;
    private static readonly Color invalidTint = new Color(1f, 0.45f, 0.45f, 1f);

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
        // 전투가 끝나 결과 화면이 떠 있으면 손패 카드를 집을 수 없음 (pointerDrag를 비워 OnDrag/OnEndDrag도 취소)
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            eventData.pointerDrag = null;
            return;
        }

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
        BringToFront();

        if (cardUI.cardType == CardType.Adjective)
            ComboManager.Instance.ShowSlots();
        else if (cardUI.cardType == CardType.Gerund && ComboManager.Instance.adjSlot.isOccupied)
            ComboManager.Instance.ShowSlots();

        // 바로 사용할 수 있는 카드면 놓을 수 있는 대상(적/플레이어) 머리 위에 화살표
        canUseOnTarget = CanUseOnTarget();
        hoverState = HoverState.None;
        if (canUseOnTarget)
            EnemyTarget.HighlightFor(cardUI);
    }

    // 형용사 슬롯이 비어 있을 때의 동명사/시너지 카드만 적·플레이어에게 사용 가능
    private bool CanUseOnTarget()
    {
        return (cardUI.cardType == CardType.Gerund || cardUI.cardType == CardType.Synergy)
            && !ComboManager.Instance.adjSlot.isOccupied;
    }

    private void OnDisable()
    {
        // 드래그 도중 비활성화되면 화살표/붉은 색이 남지 않도록
        if (isDragging) EnemyTarget.ClearHighlights();
        SetHoverState(HoverState.None);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        if (canUseOnTarget)
        {
            EnemyTarget target = EnemyTarget.FindAt(transform.position);
            if (target == null) SetHoverState(HoverState.None);
            else SetHoverState(target.Accepts(cardUI) ? HoverState.Valid : HoverState.Invalid);
        }
    }

    private void SetHoverState(HoverState state)
    {
        if (state == hoverState) return;
        hoverState = state;

        // 올바른 대상 위에서는 반투명이 풀려 "놓으면 사용된다"는 느낌을 줌
        if (canvasGroup != null && isDragging)
            canvasGroup.alpha = state == HoverState.Valid ? 1f : 0.6f;

        if (state == HoverState.Invalid)
        {
            tintImages = GetComponentsInChildren<Image>();
            tintBaseColors = new Color[tintImages.Length];
            for (int i = 0; i < tintImages.Length; i++)
            {
                tintBaseColors[i] = tintImages[i].color;
                tintImages[i].color = tintBaseColors[i] * invalidTint;
            }
        }
        else if (tintImages != null)
        {
            for (int i = 0; i < tintImages.Length; i++)
                if (tintImages[i] != null) tintImages[i].color = tintBaseColors[i];
            tintImages = null;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SetHoverState(HoverState.None);
        isDragging = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        RestoreSorting();
        EnemyTarget.ClearHighlights();

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
                // 슬롯 장착 소리
                if (SoundManager.Instance != null) {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.equipSlotSound);
                }

                slot.PlaceCard(gameObject);
                return;
            }
        }

        // 적/플레이어 판정: 원래 동작하던 Physics2D 방식 복원
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D hit in hits)
        {
            EnemyTarget target = hit.GetComponent<EnemyTarget>();
            if (target != null && (cardUI.cardType == CardType.Gerund || cardUI.cardType == CardType.Synergy))
            {
                // 형용사 슬롯에 카드가 있거나 대상이 맞지 않으면 (피해 카드→플레이어, 방어·회복 카드→적) 사용 불가 → 패로 반환
                if (ComboManager.Instance.adjSlot.isOccupied || !target.Accepts(cardUI))
                {
                    // 사용 불가로 튕겨나가는 소리
                    if (SoundManager.Instance != null) {
                        SoundManager.Instance.PlaySFX(SoundManager.Instance.equipFailSound);
                    }

                    transform.SetParent(DataManager.Instance.handArea);
                    DataManager.Instance.RearrangeHand();
                    return;
                }
                target.ReceiveCard(cardUI);
                if (cardUI.cardData != null)
                    DeckManager.Instance.DiscardCard(cardUI.cardData);
                return;
            }
        }

        // 아무것도 못 맞췄다면 다시 패로 튕겨냅니다.
        // 장착 실패 소리
        // 슬롯이 닫혀있다면(애초에 드래그해도 슬롯이 안 나왔다면) 그냥 제자리로 돌아가는 것이므로 소리를 내지 않음
        if (ComboManager.Instance.slotPanel.activeSelf) 
        {
            if (SoundManager.Instance != null) {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.equipFailSound);
            }
        }

        // 형용사 슬롯이 차있으면 슬롯 패널 유지
        if (!ComboManager.Instance.adjSlot.isOccupied)
            ComboManager.Instance.HideSlots();
        transform.SetParent(DataManager.Instance.handArea);
        DataManager.Instance.RearrangeHand();
    }

    // 드래그 중인 카드를 같은 Canvas 안의 다른 UI(슬롯 등)보다 위에 그린다
    private void BringToFront()
    {
        if (dragCanvas == null) dragCanvas = gameObject.AddComponent<Canvas>();
        dragCanvas.overrideSorting = true;
        dragCanvas.sortingLayerID = canvas.rootCanvas.sortingLayerID;
        dragCanvas.sortingOrder = canvas.rootCanvas.sortingOrder + 1;
    }

    private void RestoreSorting()
    {
        if (dragCanvas == null) return;
        Destroy(dragCanvas);
        dragCanvas = null;
    }
}
