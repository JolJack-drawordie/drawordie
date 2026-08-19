using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BattleRewardManager : MonoBehaviour
{
    // 싱글톤 인스턴스 (다른 스크립트에서 BattleRewardManager.Instance 로 쉽게 접근 가능)
    public static BattleRewardManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject rewardCardPrefab;
    [SerializeField] private Transform rewardCardParent;
    [SerializeField] private GameObject rewardUIPanel;

    private List<ICard> displayedRewards = new List<ICard>(); // 현재 화면에 뜬 보상 카드 3장

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 게임 시작 시 보상 패널은 확실하게 꺼두기
        if (rewardUIPanel != null)
        {
            rewardUIPanel.SetActive(false);
        }
    }

    // [읽기 전용 프로퍼티] 내부에서는 자유롭게 쓰고, 외부에서는 함부로 수정 못 하게 막음
    private List<ICard> MasterPool
    {
        get
        {
            return DataManager.Instance?.masterCardPool;
        }
    }

    /// 1. 전투 승리 시 호출: 두 풀을 합쳐서 무작위로 3장 추출
    public void GenerateRewardChoices()
    {
        // 1. 마스터 풀 가져오기 (필요할 때 안전하게 참조)
        var masterPool = MasterPool;

        if (masterPool == null || masterPool.Count == 0)
        {
            Debug.LogError("[BattleRewardManager] 마스터 카드 풀이 비어있습니다!");
            return;
        }

        // 2. [성능 개선 포인트] 전체 정렬(OrderBy) 대신, 
        // 무작위로 3장을 안전하게 뽑아내는 효율적인 방식 적용
        displayedRewards = GetRandomRewards(masterPool, 3);

        // UI 패널 켜기
        if (rewardUIPanel != null) rewardUIPanel.SetActive(true);

        // 기존 카드 정리 후 생성
        foreach (Transform child in rewardCardParent)
        {
            Destroy(child.gameObject);
        }

        // 3장을 화면에 생성하고 뷰 연결
        for (int i = 0; i < displayedRewards.Count; i++)
        {
            int index = i; // 클로저 이슈 방지
            GameObject cardObj = Instantiate(rewardCardPrefab, rewardCardParent);
            RewardCardView cardView = cardObj.GetComponent<RewardCardView>();

            if (cardView != null)
            {
                cardView.Setup(displayedRewards[index], (clickedCard) =>
                {
                    SelectRewardCard(index);
                });
            }
        }

    }

    /// 2. 플레이어가 보상 카드 3장 중 하나를 클릭했을 때 호출 (버튼 이벤트 연결용)
    /// <param name="selectedIndex">선택한 카드 인덱스 (0, 1, 2)</param>
    public void SelectRewardCard(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= displayedRewards.Count) return;

        ICard chosenCard = displayedRewards[selectedIndex];

        // 덱 매니저 인스턴스를 통해 바로 카드 추가 (메서드 이름은 기존 덱매니저에 맞게 수정)
        DeckManager.Instance.AddCardToDeck(chosenCard);

        CloseRewardUI();
    }

    /// 마스터 풀에서 성능 부담을 줄이며 무작위로 N장을 뽑는 헬퍼 메서드
    private List<ICard> GetRandomRewards(List<ICard> sourcePool, int count)
    {
        // 원본 풀을 훼손하지 않기 위해 복사본 생성
        List<ICard> poolCopy = new List<ICard>(sourcePool);
        List<ICard> selected = new List<ICard>();

        int targetCount = Mathf.Min(count, poolCopy.Count);

        for (int i = 0; i < targetCount; i++)
        {
            int randomIndex = Random.Range(0, poolCopy.Count);
            selected.Add(poolCopy[randomIndex]);
            poolCopy.RemoveAt(randomIndex); // 중복 뽑기 방지
        }

        return selected;
    }

    private void CloseRewardUI()
    {
        displayedRewards.Clear();
        if (rewardUIPanel != null) rewardUIPanel.SetActive(false);
        UIManager.Instance.GoToMapAfterVictory();
    }
}