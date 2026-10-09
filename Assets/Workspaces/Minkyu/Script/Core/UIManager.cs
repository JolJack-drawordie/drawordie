using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("에너지 UI")]
    public Slider energySlider;
    public TextMeshProUGUI energySliderText;
    public TextMeshProUGUI energyText;

    [Header("기본 UI")]
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerShieldText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyShieldText;

    public AnimatedBar playerHpBar;
    public AnimatedBar playerShieldBar;
    public AnimatedBar enemyHpBar;
    public AnimatedBar enemyShieldBar;

    private HpProvider playerHpProvider;
    private HpProvider enemyHpProvider;
    private ShieldProvider playerShieldProvider;
    private ShieldProvider enemyShieldProvider;

    [Header("참조")]
    public DiceManager diceManager;

    [Header("전투 결과 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardText;

    [Header("클리어 랭킹 UI")]
    public Button clearRankingButton;

    private Button defeatLobbyButton;

    public static UIManager Instance;

    private void Awake()
    {
        Instance = this;

        if (clearRankingButton != null)
        {
            clearRankingButton.onClick.AddListener(RankingPanel.Open);
            clearRankingButton.gameObject.SetActive(false);
        }

        if (playerHpBar != null)
            playerHpProvider = playerHpBar.GetComponent<HpProvider>();

        if (playerShieldBar != null)
            playerShieldProvider = playerShieldBar.GetComponent<ShieldProvider>();

        if (enemyHpBar != null)
            enemyHpProvider = enemyHpBar.GetComponent<HpProvider>();

        if (enemyShieldBar != null)
            enemyShieldProvider = enemyShieldBar.GetComponent<ShieldProvider>();
    }

    private void OnEnable()
    {
        if (DiceManager.Instance != null)
            DiceManager.Instance.OnEnergyChanged += UpdateEnergyUI;
    }

    private void OnDisable()
    {
        if (DiceManager.Instance != null)
            DiceManager.Instance.OnEnergyChanged -= UpdateEnergyUI;
    }

    private void Start()
    {
        if (DiceManager.Instance != null)
            UpdateEnergyUI(DiceManager.Instance.CurrentEnergy);
    }

    // =========================
    // 에너지 UI
    // =========================

    public void UpdateEnergyUI(int newEnergy)
    {
        int maxEnergy = DiceManager.Instance != null
            ? DiceManager.Instance.MaxEnergy
            : newEnergy;

        if (energySlider != null)
        {
            energySlider.interactable = false;
            energySlider.minValue = 0;
            energySlider.maxValue = Mathf.Max(1, maxEnergy);
            energySlider.value = newEnergy;
        }

        if (energySliderText != null)
            energySliderText.text = $"{newEnergy} / {maxEnergy}";

        if (energyText != null)
            energyText.text = "Energy : " + newEnergy;
    }

    // =========================
    // 전투 결과
    // =========================

    public void ShowResult(bool isVictory)
    {
        if (isVictory)
        {
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.DiscardHand();
                DeckManager.Instance.RefillDeckFromDiscard(true);
                DeckManager.Instance.RefillDeckFromDiscard(false);
            }

            if (rewardPanel != null)
                rewardPanel.SetActive(true);

            if (rewardText != null)
                rewardText.text = "승리! 보상을 선택하세요.";

            if (BattleRewardManager.Instance != null)
                BattleRewardManager.Instance.GenerateRewardChoices();
        }
        else
        {
            if (rewardPanel != null)
                rewardPanel.SetActive(false);

            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (resultText != null)
                resultText.text = "Defeat";

            CreateDefeatLobbyButton();
        }
    }

    public void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (defeatLobbyButton != null)
            defeatLobbyButton.gameObject.SetActive(false);
    }

    // =========================
    // 패배 후 로비 이동 버튼
    // =========================

    private void CreateDefeatLobbyButton()
    {
        if (resultPanel == null)
        {
            Debug.LogError("[UIManager] ResultPanel이 연결되지 않았습니다.");
            return;
        }

        // 이미 생성된 버튼이 있으면 중복 생성하지 않음
        if (defeatLobbyButton != null)
        {
            defeatLobbyButton.gameObject.SetActive(true);
            return;
        }

        GameObject buttonObject = new GameObject(
            "DefeatLobbyButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(resultPanel.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -60f);
        rect.sizeDelta = new Vector2(220f, 55f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.12f, 0.12f, 1f);

        defeatLobbyButton = buttonObject.GetComponent<Button>();
        defeatLobbyButton.targetGraphic = image;
        defeatLobbyButton.onClick.AddListener(GoToLobbyAfterDefeat);

        GameObject textObject = new GameObject(
            "ButtonText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonText =
            textObject.GetComponent<TextMeshProUGUI>();

        buttonText.text = "로비로 돌아가기";
        buttonText.fontSize = 24;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        Debug.Log("[UIManager] 패배 후 로비 이동 버튼을 생성했습니다.");
    }

    public void GoToLobbyAfterDefeat()
    {
        Debug.Log("[UIManager] 패배 후 LobbyScene으로 이동합니다.");

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();

        // 새 게임 시작 시 LobbyManager가 런 상태와 플레이어 스탯을 초기화함
        SceneManager.LoadScene("LobbyScene");
    }

    // =========================
    // 승리 후 맵 이동
    // =========================

    public void GoToMapAfterVictory()
    {
        bool isBossNode =
            GameFlowData.currentNodeType == MapNode.NodeType.Boss;

        if (isBossNode)
        {
            if (GameFlowData.IsFinalAct())
            {
                Debug.Log("최종 보스 클리어! LobbyScene으로 이동합니다.");

                if (rewardPanel != null)
                    rewardPanel.SetActive(false);

                if (resultPanel != null)
                    resultPanel.SetActive(false);

                if (SoundManager.Instance != null)
                    SoundManager.Instance.StopBGM();

                SceneManager.LoadScene("LobbyScene");
                return;
            }

            Debug.Log(
                $"Act {GameFlowData.currentAct} 클리어! 다음 Act로 이동합니다."
            );

            GameFlowData.MoveToNextAct();
        }

        GameFlowData.clearedNodeLevel++;

        SceneManager.LoadScene("MapScene");

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();
    }

    // =========================
    // 유닛 UI 연결
    // =========================

    public void LinkUnitToUI(UnitBase unit)
    {
        if (unit == null)
            return;

        if (unit is PlayerUnit)
        {
            if (playerHpProvider != null && playerHpBar != null)
            {
                playerHpProvider.SetTarget(unit);
                playerHpBar.SetProvider(playerHpProvider);
            }

            if (playerShieldProvider != null && playerShieldBar != null)
            {
                playerShieldProvider.SetTarget(unit);
                playerShieldBar.SetProvider(playerShieldProvider);
            }
        }
        else if (unit is EnemyUnit)
        {
            if (enemyHpProvider != null && enemyHpBar != null)
            {
                enemyHpProvider.SetTarget(unit);
                enemyHpBar.SetProvider(enemyHpProvider);
            }

            if (enemyShieldProvider != null && enemyShieldBar != null)
            {
                enemyShieldProvider.SetTarget(unit);
                enemyShieldBar.SetProvider(enemyShieldProvider);
            }
        }
    }
}