using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("참조")]
    public GameManager gameManager;
    public DiceManager diceManager;
    public UIManager uiManager;
    public PlayerUnit player;
    public EnemyUnit enemy;

    [Header("턴 정보")]
    public int turnCount = 0;
    public bool playerActionFinished = false;

    private void Start()
    {
        gameManager.StartBattle();
        if (uiManager != null) uiManager.HideResult();
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        while (!gameManager.isGameOver)
        {
            turnCount++;
            gameManager.currentState = BattleState.TurnStart;
            Debug.Log($"===== Turn {turnCount} Start =====");

            // 주사위 굴리기 버튼 대기
            diceManager.ShowRollButton();
            yield return new WaitUntil(() => diceManager.isRollFinished);

            int rolledEnergy = diceManager.CurrentEnergy;
            Debug.Log($"Turn Energy: {rolledEnergy}");

            // DataManager에 마나를 넘겨주며 카드 드로우 실행!
            if (DataManager.Instance != null)
            {
                DataManager.Instance.TriggerCardDraw(rolledEnergy);
            }

            yield return new WaitForSeconds(1.5f);

            gameManager.currentState = BattleState.PlayerTurn;
            Debug.Log("Player Turn Start - Waiting for input...");
            playerActionFinished = false;

            while (!playerActionFinished)
            {
                if (gameManager.isGameOver) yield break;

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (enemy != null && !enemy.IsDead())
                    {
                        player.Attack(enemy);
                        gameManager.CheckBattleResult();
                        if (ShowBattleResultIfGameOver()) yield break;
                    }
                    playerActionFinished = true;
                }
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.EnemyTurn;
            Debug.Log("Enemy Turn Start");

            enemy.Attack(player);
            gameManager.CheckBattleResult();

            if (ShowBattleResultIfGameOver()) yield break;

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.TurnEnd;
            Debug.Log($"===== Turn {turnCount} End =====");

            yield return new WaitForSeconds(1f);
        }
    }

    private bool ShowBattleResultIfGameOver()
    {
        if (!gameManager.isGameOver) return false;

        if (gameManager.currentState == BattleState.Victory)
            uiManager.ShowResult(true);
        else if (gameManager.currentState == BattleState.Defeat)
            uiManager.ShowResult(false);

        return true;
    }

    public void EndPlayerTurn()
    {
        playerActionFinished = true;
    }
}