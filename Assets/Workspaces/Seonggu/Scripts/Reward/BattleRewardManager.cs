using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 승리 보상: 카드 3장 중 하나를 골라 덱에 넣고, 중간보스를 잡았으면 이어서 유물 3개 중 하나를 고른다. 다 끝나면 맵으로 이동.
// 보상 후보는 노드 시드 기반 난수로 정해서, 같은 노드면 불러와도 같은 보상이 나온다.
public class BattleRewardManager : MonoBehaviour
{
    public static BattleRewardManager Instance { get; private set; }

    [Header("카드 보상 UI")]
    [SerializeField] private GameObject rewardCardPrefab;
    [SerializeField] private Transform rewardCardParent;
    [SerializeField] private GameObject rewardUIPanel;

    [Header("유물 보상 UI")]
    [SerializeField] private GameObject relicRewardPanel;

    // 유물 슬롯 3칸의 아이콘 / 이름 / 설명
    [SerializeField] private Image[] relicSlotIcons = new Image[3];
    [SerializeField] private TextMeshProUGUI[] relicNameTexts = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] relicDescriptionTexts = new TextMeshProUGUI[3];

    // 유물 아이콘 클릭 / 호버 처리 (각 아이콘에 실행 중에 붙임)
    private RelicRewardSlotView[] relicSlotViews;

    // 유물을 고르지 않고 넘어가기
    [Header("유물 보상 넘어가기")]
    [SerializeField] private Button nextButton;

    // 유물 key ↔ 아이콘 그림 (인스펙터에서 연결)
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

    // 보상 전용 난수 (노드 시드 기반, 몬스터 / 덱 섞기와 겹치지 않게 +2)
    private System.Random rewardRng;

    // 지금 보여 주는 카드 보상
    private List<ICard> displayedRewards = new List<ICard>();

    // 지금 보여 주는 유물 보상 (최대 3개)
    private List<RelicData> selectedRelics = new List<RelicData>();

    private bool isProcessingReward; // 선택을 처리하는 중 (중복 클릭 방지)
    private bool isRelicReward; // 지금 유물 보상 단계인지
    private bool isLeavingReward; // 이미 맵으로 떠나는 중 (중복 이동 방지)

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

        if (rewardUIPanel != null)
            rewardUIPanel.SetActive(false);

        if (relicRewardPanel != null)
            relicRewardPanel.SetActive(false);

        // 카드 보상처럼 유물 아이콘을 누르면 선택
        relicSlotViews =
            new RelicRewardSlotView[relicSlotIcons.Length];

        for (int i = 0; i < relicSlotIcons.Length; i++)
        {
            int index = i;

            if (relicSlotIcons[i] == null)
                continue;

            // 아이콘에만 붙여서 아이콘만 호버 / 클릭되게 함 (이름 / 설명 글자는 클릭을 받지 않음)
            GameObject iconObj = relicSlotIcons[i].gameObject;

            RelicRewardSlotView view =
                iconObj.GetComponent<RelicRewardSlotView>();

            if (view == null)
                view = iconObj.AddComponent<RelicRewardSlotView>();

            if (i < relicNameTexts.Length && relicNameTexts[i] != null)
                relicNameTexts[i].raycastTarget = false;

            if (i < relicDescriptionTexts.Length && relicDescriptionTexts[i] != null)
                relicDescriptionTexts[i].raycastTarget = false;

            view.Init(relicSlotIcons[i], () => AcquireSelectedRelic(index));
            relicSlotViews[i] = view;
        }

        // 넘어가기 버튼
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(GoToMap);
        }
        else
        {
            Debug.LogWarning(
                "[BattleRewardManager] NextButton이 Inspector에 연결되지 않았습니다."
            );
        }
    }

    private void Start()
    {
        rewardRng = new System.Random(
            GameFlowData.currentNodeSeed + 2
        );
    }

    // 보상 후보가 되는 전체 카드
    private List<ICard> MasterPool
    {
        get
        {
            return CardDataManager.Instance?.masterCardPool;
        }
    }

    // ===== 카드 보상 =====

    // 카드 보상 3장을 뽑아 화면에 보여 준다 (UIManager.ShowResult가 호출)
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

        displayedRewards = GetRandomRewards(masterPool, 3);

        isProcessingReward = false;
        isRelicReward = false;
        isLeavingReward = false;

        if (relicRewardPanel != null)
            relicRewardPanel.SetActive(false);

        if (rewardUIPanel != null)
            rewardUIPanel.SetActive(true);

        if (rewardCardParent == null || rewardCardPrefab == null)
        {
            Debug.LogError(
                "[BattleRewardManager] 카드 보상 UI가 연결되지 않았습니다."
            );
            return;
        }

        foreach (Transform child in rewardCardParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < displayedRewards.Count; i++)
        {
            int index = i;

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
                    "[BattleRewardManager] RewardCardView가 없습니다."
                );
            }
        }
    }

    // ===== 카드 보상 선택 =====

    // 고른 카드를 덱에 넣고, 중간보스면 유물 보상으로, 아니면 맵으로
    public void SelectRewardCard(int selectedIndex)
    {
        if (isProcessingReward || isLeavingReward)
            return;

        if (selectedIndex < 0 ||
            selectedIndex >= displayedRewards.Count)
            return;

        if (DeckManager.Instance == null)
        {
            Debug.LogError(
                "[BattleRewardManager] DeckManager를 찾을 수 없습니다."
            );
            return;
        }

        ICard chosenCard = displayedRewards[selectedIndex];

        isProcessingReward = true;

        DeckManager.Instance.AddCardToDeck(chosenCard);

        Debug.Log(
            $"[BattleRewardManager] 카드 보상 획득: {chosenCard.Name}"
        );

        displayedRewards.Clear();

        if (rewardUIPanel != null)
            rewardUIPanel.SetActive(false);

        // 중간보스를 잡았을 때만 유물 보상
        if (GameFlowData.currentNodeType == MapNode.NodeType.Elite)
        {
            isRelicReward = true;
            StartCoroutine(ShowRelicRewardWhenReady());
        }
        else
        {
            GoToMap();
        }
    }

    // ===== 유물 데이터 대기 =====

    // 유물 목록을 서버에서 받을 때까지 기다렸다가(최대 10초) 유물 보상 표시. 못 받으면 맵으로
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
                "[BattleRewardManager] 유물 데이터를 불러오지 못했습니다."
            );

            GoToMap();
            yield break;
        }

        ShowRelicReward();
    }

    // ===== 유물 보상 3개 표시 =====

    // 얻을 수 있는 유물 중 3개를 골라 슬롯에 보여 준다 (없으면 바로 맵으로)
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

        selectedRelics =
            RelicManager.Instance.GetRandomAvailableRelics(
                rewardRng,
                3
            );

        if (selectedRelics == null || selectedRelics.Count == 0)
        {
            Debug.LogWarning(
                "[BattleRewardManager] 획득 가능한 유물이 없습니다."
            );

            GoToMap();
            return;
        }

        if (relicRewardPanel == null)
        {
            Debug.LogError(
                "[BattleRewardManager] RelicRewardPanel이 없습니다."
            );

            GoToMap();
            return;
        }

        if (relicSlotIcons == null ||
            relicNameTexts == null ||
            relicDescriptionTexts == null ||
            relicSlotIcons.Length < 3 ||
            relicNameTexts.Length < 3 ||
            relicDescriptionTexts.Length < 3)
        {
            Debug.LogError(
                "[BattleRewardManager] 유물 슬롯 배열 크기가 부족합니다."
            );

            GoToMap();
            return;
        }

        // 슬롯별 유물 정보 표시 (부족한 칸은 비움)
        for (int i = 0; i < 3; i++)
        {
            bool hasRelic = i < selectedRelics.Count;

            if (!hasRelic || selectedRelics[i] == null)
            {
                SetRelicSlotEmpty(i);
                continue;
            }

            RelicData relic = selectedRelics[i];

            if (relicNameTexts[i] == null ||
                relicDescriptionTexts[i] == null ||
                relicSlotIcons[i] == null)
            {
                Debug.LogError(
                    $"[BattleRewardManager] 유물 슬롯 {i + 1}의 UI가 연결되지 않았습니다."
                );

                SetRelicSlotEmpty(i);
                continue;
            }

            relicNameTexts[i].text = relic.name;
            relicDescriptionTexts[i].text = relic.description;

            Sprite iconSprite = GetRelicIcon(relic.key);

            relicSlotIcons[i].sprite = iconSprite;
            relicSlotIcons[i].enabled = iconSprite != null;

            SetRelicSlotInteractable(i, true);

            Debug.Log(
                $"[BattleRewardManager] 유물 슬롯 {i + 1}: {relic.name}"
            );
        }

        // 유물 보상 패널 표시
        isProcessingReward = false;
        isRelicReward = true;
        isLeavingReward = false;

        relicRewardPanel.SetActive(true);

        Debug.Log(
            $"[BattleRewardManager] 유물 보상 {selectedRelics.Count}개 표시"
        );
    }

    // ===== 유물 아이콘 찾기 =====

    private Sprite GetRelicIcon(string relicKey)
    {
        foreach (RelicIconEntry entry in relicIcons)
        {
            if (entry != null &&
                entry.relicKey == relicKey)
            {
                return entry.icon;
            }
        }

        Debug.LogWarning(
            $"[BattleRewardManager] 유물 아이콘을 찾을 수 없습니다: {relicKey}"
        );

        return null;
    }

    // ===== 빈 슬롯 =====

    private void SetRelicSlotEmpty(int index)
    {
        if (relicNameTexts != null &&
            index < relicNameTexts.Length &&
            relicNameTexts[index] != null)
        {
            relicNameTexts[index].text = "";
        }

        if (relicDescriptionTexts != null &&
            index < relicDescriptionTexts.Length &&
            relicDescriptionTexts[index] != null)
        {
            relicDescriptionTexts[index].text = "";
        }

        if (relicSlotIcons != null &&
            index < relicSlotIcons.Length &&
            relicSlotIcons[index] != null)
        {
            relicSlotIcons[index].sprite = null;
            relicSlotIcons[index].enabled = false;
        }

        SetRelicSlotInteractable(index, false);
    }

    // ===== 유물 선택 및 획득 =====

    private void AcquireSelectedRelic(int selectedIndex)
    {
        Debug.Log(
            $"[BattleRewardManager] 유물 클릭: {selectedIndex}"
        );

        if (!isRelicReward || isProcessingReward || isLeavingReward)
            return;

        if (selectedIndex < 0 ||
            selectedIndex >= selectedRelics.Count)
            return;

        if (RelicManager.Instance == null)
        {
            Debug.LogError(
                "[BattleRewardManager] RelicManager를 찾을 수 없습니다."
            );
            return;
        }

        RelicData chosenRelic = selectedRelics[selectedIndex];

        if (chosenRelic == null)
            return;

        isProcessingReward = true;

        // 중복 획득 방지: 모든 슬롯 막기
        SetAllRelicButtonsInteractable(false);

        bool acquired = RelicManager.Instance.AcquireRelic(
            chosenRelic.key
        );

        // 획득에 실패하면 다시 고를 수 있게
        if (!acquired)
        {
            Debug.LogWarning(
                "[BattleRewardManager] 유물 획득에 실패했습니다."
            );

            isProcessingReward = false;
            SetAllRelicButtonsInteractable(true);
            return;
        }

        Debug.Log(
            $"[BattleRewardManager] 유물 보상 획득: {chosenRelic.name}"
        );

        if (RelicIconUI.Instance != null)
        {
            RelicIconUI.Instance.RefreshRelicIcons();
        }

        selectedRelics.Clear();

        GoToMap();
    }

    // ===== 유물 슬롯 선택 가능 여부 =====

    private void SetRelicSlotInteractable(int index, bool interactable)
    {
        if (relicSlotViews != null &&
            index < relicSlotViews.Length &&
            relicSlotViews[index] != null)
        {
            relicSlotViews[index].SetInteractable(interactable);
        }
    }

    private void SetAllRelicButtonsInteractable(bool interactable)
    {
        if (relicSlotViews == null)
            return;

        for (int i = 0; i < relicSlotViews.Length; i++)
        {
            // 빈 슬롯은 다시 켜지 않음
            bool hasRelic = i < selectedRelics.Count && selectedRelics[i] != null;
            SetRelicSlotInteractable(i, interactable && hasRelic);
        }
    }

    // ===== 카드 보상 무작위 추출 (중복 없이) =====

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
            poolCopy.RemoveAt(randomIndex);
        }

        return selected;
    }

    // ===== 보상 종료 및 맵 이동 =====

    private void GoToMap()
    {
        if (isLeavingReward)
            return;

        isLeavingReward = true;

        isRelicReward = false;
        isProcessingReward = false;

        selectedRelics.Clear();

        if (rewardUIPanel != null)
            rewardUIPanel.SetActive(false);

        if (relicRewardPanel != null)
            relicRewardPanel.SetActive(false);

        Debug.Log(
            "[BattleRewardManager] 보상 종료. 맵으로 이동합니다."
        );

        if (UIManager.Instance != null)
        {
            UIManager.Instance.GoToMapAfterVictory();
        }
        else
        {
            Debug.LogError(
                "[BattleRewardManager] UIManager를 찾을 수 없습니다."
            );

            isLeavingReward = false;
        }
    }

    // ===== 정리 =====

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(GoToMap);
        }

        Instance = null;
    }
}
