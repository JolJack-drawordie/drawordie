using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("기본 UI")]
    public TextMeshProUGUI energyText;
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

    public static UIManager Instance;

    private void Awake()
    {
        Instance = this;

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