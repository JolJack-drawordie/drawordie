using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("기본 UI")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI playerShieldText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI enemyShieldText;

    public Slider playerHpBar;
    public Slider playerShieldBar;
    public Slider enemyHpBar;
    public Slider enemyShieldBar;


    [Header("참조")]
    public DiceManager diceManager;
    public PlayerUnit player;
    public EnemyUnit enemy;

    [Header("전투 결과 UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (diceManager == null || player == null || enemy == null) return;
        if (energyText == null || playerHpText == null || enemyHpText == null) return;

        energyText.text = "Energy : " + diceManager.CurrentEnergy;
        playerHpText.text = "Player HP : " + player.currentHp;
        enemyHpText.text = "Enemy HP : " + enemy.currentHp;

        if (playerHpBar != null)
        {
            playerHpBar.maxValue = player.maxHp;
            playerHpBar.value = player.currentHp;
        }

        if (enemyHpBar != null)
        {
            enemyHpBar.maxValue = enemy.maxHp;
            enemyHpBar.value = enemy.currentHp;
        }

        // 플레이어 방어도 추가
        if (playerShieldBar != null && playerShieldText != null)
        {
            playerShieldBar.maxValue = player.maxHp;
            playerShieldBar.value = player.currentShield;
            playerShieldText.text = "Player Shield : " + player.currentShield;
        }
        // 적 방어도 추가
        if (enemyShieldBar != null && enemyShieldText != null)
        {
            enemyShieldBar.maxValue = enemy.maxHp;
            enemyShieldBar.value = enemy.currentShield;
            enemyShieldText.text = "Enemy Shield : " + enemy.currentShield;
        }
    }

    public void ShowResult(bool isVictory)
    {
        resultPanel.SetActive(true);

        if (isVictory)
        {
            resultText.text = "Victory";
        }
        else
        {
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
    }
}