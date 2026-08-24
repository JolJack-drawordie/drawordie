using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PileUI : MonoBehaviour, IPointerClickHandler
{
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
    public TextMeshProUGUI countText; // 남은 카드 장수를 띄울 텍스트

    private void Update()
    {
        UpdateCardCount();
    }

    // 매 프레임 혹은 데이터가 갱신될 때 카드 장수 업데이트
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

    // 3. 기존 OnClickPile 대신 이걸 사용 (유니티가 클릭할 때 알아서 실행해줌)
    public void OnPointerClick(PointerEventData eventData)
    {
        // 좌클릭일 때만 작동하게 하고 싶다면 아래 조건 추가 가능
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"{pileType} 더미 클릭됨! (IPointerClickHandler 방식)");

            if (DeckManager.Instance == null) return;

            // 1. 타입에 따라 DeckManager에서 리스트 참조 가져오기
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

            // 2. 팝업창 열기 (DeckViewerUI가 싱글턴이거나 참조되어 있다는 가정하에)
            if (targetList != null)
            {
                DeckViewerUI.Instance.OpenViewer(targetList, pileType);
            }
        }
    }
}