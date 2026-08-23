using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PileUI : MonoBehaviour
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

    // 더미 버튼을 클릭했을 때 호출될 함수 (나중에 팝업창 연결용)
    public void OnClickPile()
    {
        Debug.Log($"{pileType} 더미 클릭됨!");

        // TODO: 여기에 팝업창을 띄우고 해당 리스트(AdjectiveDrawPile 등)를 전달하는 로직 연결하면 됨!
    }
}