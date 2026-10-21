using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardManager : MonoBehaviour
{
    // 싱글톤 인스턴스 (다른 스크립트에서 BattleRewardManager.Instance 로 접근 가능)
    public static BattleRewardManager Instance { get; private set; }

    [Header("카드 보상 UI")]
    [SerializeField] private GameObject rewardCardPrefab;
    [SerializeField] private Transform rewardCardParent;
    [SerializeField] private GameObject rewardUIPanel;

    [Header("유물 보상 UI")]
    [SerializeField] private GameObject relicRewardPanel;
    [SerializeField] private Image relicIcon;
    [SerializeField] private TextMeshProUGUI relicNameText;
    [SerializeField] private TextMeshProUGUI relicDescriptionText;
    [SerializeField] private Button acquireRelicButton;

    [Header("유물 아이콘 연결")]
    [SerializeField]
    private List<RelicIconEntry> relicIcons =
        new List<RelicIconEntry>();

    [Serializable]
    public class RelicIconEntry
    {
        public string relicKey;
        public Sprite icon;
    }

    // 보상 시드 기반 무작위 난수 생성기
    private System.Random rewardRng;

    private List<ICard> displayedRewards = new List<ICard>(); // 현재 화면에 표시된 보상 카드 3장
    private RelicData selectedRelic;

    private bool isProcessingReward;
    private bool isRelicReward;

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

        // 초기화 시 UI 패널들을 비활성화
        if (rewardUIPanel != null)
        {
            rewardUIPanel.SetActive(false);
        }

        if (relicRewardPanel != null)
        {
            relicRewardPanel.SetActive(false);
        }

        // 유물 획득 버튼 이벤트 리스너 등록
        if (acquireRelicButton != null)
        {
            acquireRelicButton.onClick.AddListener(AcquireSelectedRelic);
        }
    }

    private void Start()
    {
        int nodeSeed = GameFlowData.currentNodeSeed;

        // 노드마다 고유 시드를 부여해 동일 노드에서는 같은 무작위 순서 사용
        rewardRng = new System.Random(nodeSeed + 2);
    }

    // [읽기 전용 프로퍼티] 마스터 카드 롤 참조 (안전하게 접근)
    private List<ICard> MasterPool
    {
        get
        {
            return CardDataManager.Instance?.masterCardPool;
        }
    }

    /// 전투 승리 시 호출: 마스터 카드 롤에서 무작위 보상 카드 3장 생성
    public void GenerateRewardChoices()
    {
        var masterPool = MasterPool;

        if (masterPool == null || masterPool.Count == 0)
        {
            Debug.LogError(
                "[BattleRewardManager] 보상용 카드 풀이 비어 있습니다."
            );
            return;
        }

        if (rewardRng == null)
        {
            rewardRng = new System.Random(
                GameFlowData.currentNodeSeed + 2
            );
        }

        // 3개의 보상 카드 무작위 선별
        displayedRewards = GetRandomRewards(masterPool, 3);
        isProcessingReward = false;

        if (relicRewardPanel != null)
        {
            relicRewardPanel.SetActive(false);
        }

        if (rewardUIPanel != null)
        {
            rewardUIPanel.SetActive(true);
        }

        if (rewardCardParent == null || rewardCardPrefab == null)
        {
            Debug.LogError(
                "[BattleRewardManager] 카드 보상 UI가 연결되지 않았습니다."
            );
            return;
        }

        // 기존 생성된 보상 카드 프리팹 제거
        foreach (Transform child in rewardCardParent)
        {
            Destroy(child.gameObject);
        }

        // 선별된 3개의 카드를 UI 화면에 생성 및 이벤트 연결
        for (int i = 0; i < displayedRewards.Count; i++)
        {
            int index = i; // 클로저 이슈 방지

            GameObject cardObj = Instantiate(
                rewardCardPrefab,
                rewardCardParent
            );

            RewardCardView cardView =
                cardObj.GetComponent<RewardCardView>();

            if (cardView != null)
            {
                cardView.Setup(displayedRewards[index], clickedCard =>
                {
                    SelectRewardCard(index);
                });
            }
            else
            {
                Debug.LogError(
                    "[BattleRewardManager] 보상 카드에 " +
                    "RewardCardView가 없습니다."
                );
            }
        }
    }

    /// 플레이어가 보상 카드 3장 중 하나를 선택했을 때 호출 (덱에 추가)
    public void SelectRewardCard(int selectedIndex)
    {
        if (isProcessingReward)
        {
            return;
        }

        if (selectedIndex < 0 ||
            selectedIndex >= displayedRewards.Count)
        {
            return;
        }

        if (DeckManager.Instance == null)
        {
            Debug.LogError(
                "[BattleRewardManager] DeckManager를 찾을 수 없습니다."
            );
            return;
        }

        ICard chosenCard = displayedRewards[selectedIndex];

        isProcessingReward = true;

        // 덱 매니저를 통해 선택한 카드 추가
        DeckManager.Instance.AddCardToDeck(chosenCard);

        Debug.Log(
            $"[BattleRewardManager] 카드 보상 획득: {chosenCard.Name}"
        );

        displayedRewards.Clear();

        if (rewardUIPanel != null)
        {
            rewardUIPanel.SetActive(false);
        }

        // 엘리트와 보스 노드일 경우 추가로 유물 보상 팝업을 표시
        if (GameFlowData.currentNodeType == MapNode.NodeType.Elite ||
            GameFlowData.currentNodeType == MapNode.NodeType.Boss)
        {
            isRelicReward = true;
            StartCoroutine(ShowRelicRewardWhenReady());
        }
        else
        {
            GoToMap();
        }
    }

    // 서버의 유물 목록 데이터 로드가 완료될 때까지 대기 후 팝업 연출
    private IEnumerator ShowRelicRewardWhenReady()
    {
        float elapsed = 0f;
        const float timeoutSeconds = 10f;

        while (RelicManager.Instance != null &&
               (RelicManager.Instance.AllRelics == null ||
                RelicManager.Instance.AllRelics.Count == 0) &&
               elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (RelicManager.Instance == null ||
            RelicManager.Instance.AllRelics == null ||
            RelicManager.Instance.AllRelics.Count == 0)
        {
            Debug.LogWarning(
                "[BattleRewardManager] 유물 데이터를 불러오지 못했습니다. " +
                "맵으로 이동합니다."
            );

            GoToMap();
            yield break;
        }

        ShowRelicReward();
    }

    // 획득 가능한 무작위 유물 1개를 팝업창에 출력
    private void ShowRelicReward()
    {
        if (RelicManager.Instance == null)
        {
            Debug.LogError(
                "[BattleRewardManager] RelicManager를 찾을 수 없습니다."
            );

            GoToMap();
            return;
        }

        if (rewardRng == null)
        {
            rewardRng = new System.Random(
                GameFlowData.currentNodeSeed + 2
            );
        }

        // 미보유 유물 중 하나 추출
        selectedRelic =
            RelicManager.Instance.GetRandomAvailableRelic(rewardRng);

        if (selectedRelic == null)
        {
            Debug.LogWarning(
                "[BattleRewardManager] 획득 가능한 유물이 없습니다."
            );

            GoToMap();
            return;
        }

        if (relicRewardPanel == null ||
            relicNameText == null ||
            relicDescriptionText == null ||
            acquireRelicButton == null)
        {
            Debug.LogError(
                "[BattleRewardManager] 유물 보상 UI가 연결되지 않았습니다."
            );

            GoToMap();
            return;
        }

        relicNameText.text = selectedRelic.name;
        relicDescriptionText.text = selectedRelic.description;

        // 유물 키에 해당하는 아이콘 스프라이트 검색 및 적용
        Sprite iconSprite = null;

        foreach (RelicIconEntry entry in relicIcons)
        {
            if (entry != null &&
                entry.relicKey == selectedRelic.key)
            {
                iconSprite = entry.icon;
                break;
            }
        }

        if (relicIcon != null)
        {
            relicIcon.sprite = iconSprite;
            relicIcon.enabled = iconSprite != null;
        }

        acquireRelicButton.interactable = true;
        relicRewardPanel.SetActive(true);

        Debug.Log(
            $"[BattleRewardManager] 유물 보상 표시: {selectedRelic.name}"
        );
    }

    // 유물 보상 창에서 획득 버튼 클릭 시 실행
    private void AcquireSelectedRelic()
    {
        if (!isRelicReward || selectedRelic == null)
        {
            return;
        }

        if (RelicManager.Instance == null)
        {
            Debug.LogError(
                "[BattleRewardManager] RelicManager를 찾을 수 없습니다."
            );
            return;
        }

        acquireRelicButton.interactable = false;

        // 유물 매니저를 통해 유물 실제 획득 및 효과 적용
        bool acquired = RelicManager.Instance.AcquireRelic(
            selectedRelic.key
        );

        if (!acquired)
        {
            Debug.LogWarning(
                "[BattleRewardManager] 유물 획득에 실패했습니다."
            );

            acquireRelicButton.interactable = true;
            return;
        }

        Debug.Log(
            $"[BattleRewardManager] 유물 보상 획득: {selectedRelic.name}"
        );

        if (RelicIconUI.Instance != null)
        {
            RelicIconUI.Instance.RefreshRelicIcons();
        }

        selectedRelic = null;
        GoToMap();
    }

    // 카드 풀에서 원본 훼손 없이 중복 없는 N개의 보상 카드 무작위 추출
    private List<ICard> GetRandomRewards(
        List<ICard> sourcePool,
        int count)
    {
        List<ICard> poolCopy = new List<ICard>(sourcePool);
        List<ICard> selected = new List<ICard>();

        int targetCount = Mathf.Min(count, poolCopy.Count);

        for (int i = 0; i < targetCount; i++)
        {
            int randomIndex = rewardRng.Next(0, poolCopy.Count);

            selected.Add(poolCopy[randomIndex]);
            poolCopy.RemoveAt(randomIndex); // 중복 선택 방지
        }

        return selected;
    }

    // 모든 보상 처리가 끝난 뒤 UI 닫기 및 맵 씬으로 전환
    private void GoToMap()
    {
        isRelicReward = false;
        isProcessingReward = false;
        selectedRelic = null;

        if (rewardUIPanel != null)
        {
            rewardUIPanel.SetActive(false);
        }

        if (relicRewardPanel != null)
        {
            relicRewardPanel.SetActive(false);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.GoToMapAfterVictory();
        }
        else
        {
            Debug.LogError(
                "[BattleRewardManager] UIManager를 찾을 수 없습니다."
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        // 버튼 클릭 이벤트 해제
        if (acquireRelicButton != null)
        {
            acquireRelicButton.onClick.RemoveListener(
                AcquireSelectedRelic
            );
        }

        Instance = null;
    }
}