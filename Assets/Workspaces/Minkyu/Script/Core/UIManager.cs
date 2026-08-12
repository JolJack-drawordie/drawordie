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
    public PlayerUnit player;
    public EnemyUnit enemy;

    [Header("전투 결과 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public GameObject rewardPanel;
    public TextMeshProUGUI rewardText;

    public static UIManager Instance;

    private void Awake()
    {
        Instance = this;
        playerHpProvider = playerHpBar.GetComponent<HpProvider>();
        playerShieldProvider = playerShieldBar.GetComponent<ShieldProvider>();
        enemyHpProvider = enemyHpBar.GetComponent <HpProvider>();
        enemyShieldProvider = enemyShieldBar.GetComponent<ShieldProvider>();
    }

    private void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (diceManager == null || player == null || enemy == null) return;
        if (energyText == null || playerHpText == null || enemyHpText == null) return;

        energyText.text = "Energy : " + diceManager.CurrentEnergy;

    }

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

    public void GoToMapAfterVictory()
    {
        GameFlowData.clearedNodeLevel++;
        SceneManager.LoadScene("MapScene");
        // 배경 음악 정지
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
        }
    }

    public void LinkUnitToUI(UnitBase unit)
    {
        if (unit is PlayerUnit) // 유닛이 플레이어라면?
        {
            playerHpProvider.SetTarget(unit);
            playerShieldProvider.SetTarget(unit);
            playerHpBar.SetProvider(playerHpProvider);
            playerShieldBar.SetProvider(playerShieldProvider);
        }
        else if (unit is EnemyUnit) // 유닛이 적이라면?
        {
            enemyHpProvider.SetTarget(unit);
            enemyShieldProvider.SetTarget(unit);
            enemyHpBar.SetProvider(enemyHpProvider);
            enemyShieldBar.SetProvider(enemyShieldProvider);
        }
    }
}
