using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI enemyHpText;

    public Slider playerHpBar;
    public Slider enemyHpBar;

    public DiceManager diceManager;
    public PlayerUnit player;
    public EnemyUnit enemy;

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
}