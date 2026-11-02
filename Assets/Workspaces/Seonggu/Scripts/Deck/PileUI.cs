using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 전투 화면의 카드 더미(형용사 / 동명사의 뽑을 더미, 버린 더미). 남은 카드 수를 표시하고, 누르면 덱 보기 팝업을 연다.
// 마우스를 올리거나 팝업으로 보고 있는 동안은 카드 팩이 열린 그림으로 바뀐다.
public class PileUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // 더미 종류
    public enum PileType
    {
        AdjectiveDraw,
        AdjectiveDiscard,
        GerundDraw,
        GerundDiscard
    }

    [Header("더미 설정")]
    public PileType pileType;

    [Header("UI 컴포넌트")]
    public TextMeshProUGUI countText; // 남은 카드 수

    // 카드 팩 그림 (평소: 닫힌 팩 / 마우스를 올렸거나 보고 있을 때: 열린 팩)
    [Header("카드팩 이미지 (호버/클릭 시 열림)")]
    public Image packImage;
    public Sprite closedSprite;
    public Sprite openSprite;

    private bool isHovered;
    private bool isViewing; // 이 더미를 팝업으로 보고 있는지

    private void Awake()
    {
        if (packImage == null) packImage = GetComponent<Image>();
        UpdatePackSprite();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        // 호버 효과음
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.cardHoverSound, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // 마우스를 올렸거나 팝업으로 보고 있으면 열린 팩 (팝업이 닫혔으면 보고 있지 않은 것으로)
    private void UpdatePackSprite()
    {
        if (isViewing && (DeckViewerUI.Instance == null || DeckViewerUI.Instance.viewerPanel == null || !DeckViewerUI.Instance.viewerPanel.activeSelf))
            isViewing = false;

        Sprite sprite = (isHovered || isViewing) ? openSprite : closedSprite;
        if (packImage != null && sprite != null && packImage.sprite != sprite)
            packImage.sprite = sprite;
    }

    private void Update()
    {
        UpdateCardCount();
        UpdatePackSprite();
    }

    // 남은 카드 수 갱신
    private void UpdateCardCount()
    {
        if (DeckManager.Instance == null) return;

        int count = 0;

        switch (pileType)
        {
            case PileType.AdjectiveDraw:
                count = DeckManager.Instance.AdjectiveDrawPile.Count;
                break;
            case PileType.AdjectiveDiscard:
                count = DeckManager.Instance.AdjectiveDiscardPile.Count;
                break;
            case PileType.GerundDraw:
                count = DeckManager.Instance.GerundDrawPile.Count;
                break;
            case PileType.GerundDiscard:
                count = DeckManager.Instance.GerundDiscardPile.Count;
                break;
        }

        if (countText != null)
        {
            countText.text = count.ToString();
        }
    }

    // 왼쪽 클릭으로 덱 보기 팝업 열기
    public void OnPointerClick(PointerEventData eventData)
    {
        // 전투가 끝나 결과 화면이 떠 있으면 열 수 없음
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // 클릭 효과음
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.drawCardSound, true);

            Debug.Log($"{pileType} 더미 클릭됨! (IPointerClickHandler 방식)");

            if (DeckManager.Instance == null) return;

            // 이 더미의 카드 목록
            List<ICard> targetList = null;

            switch (pileType)
            {
                case PileType.AdjectiveDraw:
                    targetList = DeckManager.Instance.AdjectiveDrawPile;
                    break;
                case PileType.AdjectiveDiscard:
                    targetList = DeckManager.Instance.AdjectiveDiscardPile;
                    break;
                case PileType.GerundDraw:
                    targetList = DeckManager.Instance.GerundDrawPile;
                    break;
                case PileType.GerundDiscard:
                    targetList = DeckManager.Instance.GerundDiscardPile;
                    break;
            }

            // 팝업 열기
            if (targetList != null)
            {
                DeckViewerUI.Instance.OpenViewer(targetList, pileType);
                isViewing = true;
            }
        }
    }
}