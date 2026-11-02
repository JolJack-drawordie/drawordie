using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 덱 보기 팝업. 카드 더미(PileUI)를 누르면 그 더미에 든 카드들을 목록으로 보여 준다.
public class DeckViewerUI : MonoBehaviour
{
    public static DeckViewerUI Instance { get; private set; }

    [Header("UI 연결")]
    public GameObject viewerPanel; // 팝업 전체 (켜고 끄기)
    public Transform contentParent; // 스크롤 뷰 안의 Content (카드가 들어갈 곳)
    public GameObject cardUIPrefab; // 목록에 보여 줄 카드 프리팹 (ViewerCardUI)
    public TextMeshProUGUI titleText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 처음엔 팝업을 꺼 둠
        viewerPanel.SetActive(false);
    }

    // 카드 목록을 받아 팝업을 연다
    public void OpenViewer(List<ICard> targetCards, PileUI.PileType pileType)
    {
        if (viewerPanel == null || contentParent == null || cardUIPrefab == null)
        {
            Debug.LogError("[DeckViewerUI] 연결되지 않은 UI 컴포넌트가 있습니다!");
            return;
        }

        viewerPanel.SetActive(true);

        // 더미 종류에 맞는 제목
        if (titleText != null)
        {
            switch (pileType)
            {
                case PileUI.PileType.AdjectiveDraw:
                    titleText.text = "형용사 뽑을 덱";
                    break;
                case PileUI.PileType.AdjectiveDiscard:
                    titleText.text = "형용사 버린 덱";
                    break;
                case PileUI.PileType.GerundDraw:
                    titleText.text = "동명사 뽑을 덱";
                    break;
                case PileUI.PileType.GerundDiscard:
                    titleText.text = "동명사 버린 덱";
                    break;
            }
        }

        // 이전에 보여 준 카드를 지우고
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 지금 더미의 카드를 하나씩 만든다
        foreach (var card in targetCards)
        {
            GameObject cardObj = Instantiate(cardUIPrefab, contentParent);

            ViewerCardUI viewerCardUI = cardObj.GetComponent<ViewerCardUI>();
            if (viewerCardUI != null)
            {
                viewerCardUI.Setup(card);
            }
        }
    }

    // 닫기 버튼 (onClick에 연결)
    public void CloseViewer()
    {
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }
    }
}