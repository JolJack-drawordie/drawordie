using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("에너지 UI")]
    public Slider energySlider;               // 원형 게이지 (남은 에너지 / 이번 턴 에너지)
    public TextMeshProUGUI energySliderText;  // 게이지 가운데 숫자
    public TextMeshProUGUI energyText;        // 예전 텍스트 UI (게이지가 없는 씬용)

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

    // 게임 클리어 화면의 랭킹 버튼 (Tools > Ranking > 클리어 화면 랭킹 버튼 배치 로 연결, 클리어 시에만 표시)
    // 비워두면 클리어 시 코드로 생성
    public Button clearRankingButton;

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
        {
            DiceManager.Instance.OnEnergyChanged += UpdateEnergyUI;
        }
    }

    private void OnDisable()
    {
        if (DiceManager.Instance != null)
        {
            DiceManager.Instance.OnEnergyChanged -= UpdateEnergyUI;
        }
    }

    private void Start()
    {
        if (DiceManager.Instance != null)
        {
            UpdateEnergyUI(DiceManager.Instance.CurrentEnergy);
        }
    }

    // =========================
    // Energy UI
    // =========================

    public void UpdateEnergyUI(int newEnergy)
    {
        int maxEnergy = DiceManager.Instance != null ? DiceManager.Instance.MaxEnergy : newEnergy;

        if (energySlider != null)
        {
            energySlider.interactable = false; // 표시 전용 (드래그로 값 변경 방지)
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
            DeckManager.Instance.DiscardHand();
            DeckManager.Instance.RefillDeckFromDiscard(true);
            DeckManager.Instance.RefillDeckFromDiscard(false);

            rewardPanel.SetActive(true);
            rewardText.text = "승리! 보상을 선택하세요.";

            BattleRewardManager.Instance.GenerateRewardChoices();
        }
        else
        {
            resultPanel.SetActive(true);
            resultText.text = "Defeat";
        }
    }

    public void HideResult()
    {
        resultPanel.SetActive(false);
    }

    // =========================
    // 승리 후 이동
    // =========================

    public void GoToMapAfterVictory()
    {
        // 현재 클리어한 노드가 보스인지 확인
        bool isBossNode =
            GameFlowData.currentNodeType == MapNode.NodeType.Boss;

        // =========================
        // 보스 노드 클리어
        // =========================

        if (isBossNode)
        {
            // =========================
            // Act 3 최종 보스 클리어
            // =========================

            if (GameFlowData.IsFinalAct())
            {
                Debug.Log(
                    "최종 보스 클리어! " +
                    "LobbyScene으로 이동합니다."
                );

                // 보상 패널 닫기
                if (rewardPanel != null)
                    rewardPanel.SetActive(false);

                // 결과 패널 닫기
                if (resultPanel != null)
                    resultPanel.SetActive(false);

                // 전투 BGM 정지
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.StopBGM();
                }

                // 로비 씬으로 이동
                SceneManager.LoadScene("LobbyScene");

                // 클리어 화면에서 랭킹을 볼 수 있도록 랭킹 버튼 표시
                if (clearRankingButton != null)
                {
                    clearRankingButton.gameObject.SetActive(true);
                }
                else if (resultPanel != null)
                {
                    clearRankingButton = RankingUIFactory.CreateOpenButton(
                        resultPanel.transform,
                        new Vector2(0f, -130f),
                        new Vector2(220f, 50f),
                        28f);
                }

                return;
            }

            // =========================
            // Act 1 또는 Act 2 보스 클리어
            // =========================

            Debug.Log(
                $"Act {GameFlowData.currentAct} 클리어! " +
                "다음 Act로 이동합니다."
            );

            GameFlowData.MoveToNextAct();
        }

        // =========================
        // 일반 노드 또는 엘리트 노드 클리어
        // =========================

        GameFlowData.clearedNodeLevel++;

        // 다음 Act 또는 현재 Act의 맵으로 이동
        SceneManager.LoadScene("MapScene");

        // 배경 음악 정지
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }
    }

    // =========================
    // Unit UI 연결
    // =========================

    public void LinkUnitToUI(UnitBase unit)
    {
        if (unit is PlayerUnit)
        {
            playerHpProvider.SetTarget(unit);
            playerShieldProvider.SetTarget(unit);

            playerHpBar.SetProvider(playerHpProvider);
            playerShieldBar.SetProvider(playerShieldProvider);
        }
        else if (unit is EnemyUnit)
        {
            enemyHpProvider.SetTarget(unit);
            enemyShieldProvider.SetTarget(unit);

            enemyHpBar.SetProvider(enemyHpProvider);
            enemyShieldBar.SetProvider(enemyShieldProvider);
        }
    }
}