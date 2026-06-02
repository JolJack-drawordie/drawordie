using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("기본 UI")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI enemyHpText;

    public Slider playerHpBar;
    public Slider enemyHpBar;

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
        energyText.text = "Energy : " + diceManager.CurrentEnergy;
        playerHpText.text = "Player HP : " + player.currentHp;
        enemyHpText.text = "Enemy HP : " + enemy.currentHp;

        playerHpBar.maxValue = player.maxHp;
        playerHpBar.value = player.currentHp;

        enemyHpBar.maxValue = enemy.maxHp;
        enemyHpBar.value = enemy.currentHp;
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