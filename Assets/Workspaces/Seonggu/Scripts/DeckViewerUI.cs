using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static PileUI;

public class DeckViewerUI : MonoBehaviour
{
    // 전역에서 쉽게 부를 수 있게 싱글턴 패턴 적용 (선택사항이지만 편함!)
    public static DeckViewerUI Instance { get; private set; }

    [Header("UI 연결")]
    public GameObject viewerPanel;      // 팝업창 전체 패널 (켜고 끄기용)
    public Transform contentParent;     // Scroll View 안의 Content 오브젝트
    public GameObject cardUIPrefab;     // 카드 UI 프리팹 (기존에 쓰던 거)
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

        // 게임 시작할 때는 팝업창 꺼두기
        viewerPanel.SetActive(false);
    }

    // 덱 리스트를 받아서 팝업을 여는 핵심 메서드
    public void OpenViewer(List<ICard> targetCards, PileUI.PileType pileType)
    {
        if (viewerPanel == null || contentParent == null || cardUIPrefab == null)
        {
            Debug.LogError("[DeckViewerUI] 연결되지 않은 UI 컴포넌트가 있습니다!");
            return;
        }

        // 1. 팝업 창 켜기
        viewerPanel.SetActive(true);

        // 👈 [추가] 타입에 따라서 팝업 타이틀 이름 다르게 띄워주기
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

        // 2. 이전에 남아있던 카드 UI 싹 지우기 (초기화)
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 3. 현재 덱 리스트에 있는 카드들을 돌면서 UI 생성
        foreach (var card in targetCards)
        {
            // 컨텐츠 부모 아래에 카드 프리팹 생성
            GameObject cardObj = Instantiate(cardUIPrefab, contentParent);

            ViewerCardUI viewerCardUI = cardObj.GetComponent<ViewerCardUI>();
            if (viewerCardUI != null)
            {
                // 아까 바꾼 Setup 메서드 호출
                viewerCardUI.Setup(card);
            }
        }
    }

    // 팝업 닫기 버튼에 연결할 메서드
    public void CloseViewer()
    {
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }
    }
}