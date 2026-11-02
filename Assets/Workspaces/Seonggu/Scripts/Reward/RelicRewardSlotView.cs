using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 유물 보상 아이콘. 카드 보상처럼 아이콘을 누르면 선택된다.
// 마우스를 올리면 카드 호버 소리와 함께 커지고 금색 테두리가 생긴다. BattleRewardManager가 실행 중에 각 유물 아이콘에 붙인다.
public class RelicRewardSlotView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const float HoverScale = 1.15f;
    private static readonly Color HighlightColor = new Color(1f, 0.85f, 0.35f, 0.9f);

    private Action onSelected;
    private Outline highlight;
    private Vector3 targetScale = Vector3.one;
    private bool interactable;

    // 아이콘에 금색 테두리를 준비하고, 고를 때 부를 함수를 연결 (처음엔 고를 수 없음)
    public void Init(Image icon, Action onSelected)
    {
        this.onSelected = onSelected;

        if (icon != null)
        {
            icon.raycastTarget = true;
            highlight = icon.GetComponent<Outline>();
            if (highlight == null) highlight = icon.gameObject.AddComponent<Outline>();
            highlight.effectColor = HighlightColor;
            highlight.effectDistance = new Vector2(4f, -4f);
        }

        SetInteractable(false);
    }

    // 유물을 고를 수 있는 상태인지 (카드를 고른 뒤에 열림)
    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!value) SetHovered(false);
    }

    private void OnDisable()
    {
        // 패널이 닫혔다 다시 열릴 때 커진 상태로 남지 않게
        SetHovered(false);
        transform.localScale = Vector3.one;
    }

    // 목표 크기로 부드럽게 (일시정지 중에도 동작하게 실제 시간 기준)
    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 15f);
    }

    // 커짐 + 금색 테두리 켜기 / 끄기
    private void SetHovered(bool hovered)
    {
        targetScale = hovered ? Vector3.one * HoverScale : Vector3.one;
        if (highlight != null) highlight.enabled = hovered;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;

        SetHovered(true);

        if (SoundManager.Instance != null && SoundManager.Instance.cardHoverSound != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.cardHoverSound, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        onSelected?.Invoke();
    }
}
