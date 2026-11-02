using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 손패 카드 끌기. 놓은 곳에 따라 조합 슬롯에 넣거나, 적 / 플레이어에게 사용하거나, 손패로 돌려보낸다.
// 끄는 동안 쓸 수 있는 대상 머리 위에 화살표를 띄우고, 대상 위에 올리면 쓸 수 있으면 선명하게 / 못 쓰면 붉게 보인다.
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private CardUI cardUI;

    public CardSlot currentSlot; // 지금 들어가 있는 조합 슬롯 (없으면 null)
    public bool isDragging = false;

    // 끄는 동안에만 붙이는 정렬용 Canvas (슬롯 패널보다 위에 그리기 위함)
    private Canvas dragCanvas;

    // 끄는 중 대상 위에 올렸을 때 반응 (올바른 대상: 선명하게 / 잘못된 대상: 붉게)
    private enum HoverState { None, Valid, Invalid }
    private HoverState hoverState;
    private bool canUseOnTarget; // 이 카드를 지금 적 / 플레이어에게 바로 쓸 수 있는지
    private Image[] tintImages;
    private Color[] tintBaseColors;
    private static readonly Color invalidTint = new Color(1f, 0.45f, 0.45f, 1f);

    // 모든 준비가 끝난 Start에서 설정 (Awake 시점에는 부모 Canvas가 아직 정해지지 않을 수 있음)
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        cardUI = GetComponent<CardUI>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 카드 드롭 판정용 충돌 박스 (카드 크기에 맞춤)
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true; 
        box.size = new Vector2(250, 360);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 전투가 끝나 결과 화면이 떠 있으면 집을 수 없음 (pointerDrag를 비워 OnDrag / OnEndDrag도 취소)
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
            // 슬롯의 자식에서 손패로 올려야 끄는 좌표계가 Canvas 기준으로 통일됨
            transform.SetParent(DataManager.Instance.handArea);
        }

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        BringToFront();

        // 형용사 카드면 조합 슬롯을 보여 주고, 동명사 카드는 형용사 칸이 차 있을 때만
        if (cardUI.cardType == CardType.Adjective)
            ComboManager.Instance.ShowSlots();
        else if (cardUI.cardType == CardType.Gerund && ComboManager.Instance.adjSlot.isOccupied)
            ComboManager.Instance.ShowSlots();

        // 바로 쓸 수 있는 카드면 놓을 수 있는 대상(적 / 플레이어) 머리 위에 화살표
        canUseOnTarget = CanUseOnTarget();
        hoverState = HoverState.None;
        if (canUseOnTarget)
            EnemyTarget.HighlightFor(cardUI);
    }

    // 형용사 칸이 비어 있을 때의 동명사 / 조합 카드만 적 · 플레이어에게 쓸 수 있음
    private bool CanUseOnTarget()
    {
        return (cardUI.cardType == CardType.Gerund || cardUI.cardType == CardType.Synergy)
            && !ComboManager.Instance.adjSlot.isOccupied;
    }

    private void OnDisable()
    {
        // 끄는 도중 꺼지면 화살표 / 붉은 색이 남지 않도록
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

        // 올바른 대상 위에서는 반투명이 풀려 "놓으면 사용된다"는 느낌을 준다
        if (canvasGroup != null && isDragging)
            canvasGroup.alpha = state == HoverState.Valid ? 1f : 0.6f;

        // 잘못된 대상이면 카드 전체를 붉게 (원래 색은 기억했다가 되돌림)
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

        // 1) 조합 슬롯 판정: 화면 좌표가 슬롯 안인지 직접 확인 (Canvas 크기 배율을 자동으로 처리)
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

        // 2) 적 / 플레이어 판정 (Physics2D 충돌 박스)
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        foreach (Collider2D hit in hits)
        {
            EnemyTarget target = hit.GetComponent<EnemyTarget>();
            if (target != null && (cardUI.cardType == CardType.Gerund || cardUI.cardType == CardType.Synergy))
            {
                // 형용사 칸에 카드가 있거나 대상이 맞지 않으면(피해 카드 → 플레이어, 방어 · 회복 카드 → 적) 사용 불가 → 손패로
                if (ComboManager.Instance.adjSlot.isOccupied || !target.Accepts(cardUI))
                {
                    // 사용 불가로 튕겨 나가는 소리
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

        // 3) 아무 데도 맞지 않으면 손패로 돌아감
        // 조합 슬롯이 열려 있었을 때만 실패 소리 (슬롯이 안 열렸으면 그냥 제자리로 돌아가는 것)
        if (ComboManager.Instance.slotPanel.activeSelf) 
        {
            if (SoundManager.Instance != null) {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.equipFailSound);
            }
        }

        // 형용사 칸이 차 있으면 슬롯 패널은 그대로 둠
        if (!ComboManager.Instance.adjSlot.isOccupied)
            ComboManager.Instance.HideSlots();
        transform.SetParent(DataManager.Instance.handArea);
        DataManager.Instance.RearrangeHand();
    }

    // 끄는 중인 카드를 같은 Canvas 안의 다른 UI(슬롯 등)보다 위에 그린다
    private void BringToFront()
    {
        if (dragCanvas == null) dragCanvas = gameObject.AddComponent<Canvas>();
        dragCanvas.overrideSorting = true;
        dragCanvas.sortingLayerID = canvas.rootCanvas.sortingLayerID;
        dragCanvas.sortingOrder = canvas.rootCanvas.sortingOrder + 1;
    }

    // 정렬용 Canvas 제거
    private void RestoreSorting()
    {
        if (dragCanvas == null) return;
        Destroy(dragCanvas);
        dragCanvas = null;
    }
}
