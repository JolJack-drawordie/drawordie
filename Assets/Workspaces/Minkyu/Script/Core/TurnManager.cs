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

        if (uiManager != null)
        {
            uiManager.HideResult();
        }

        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        while (!gameManager.isGameOver)
        {
            turnCount++;
            gameManager.currentState = BattleState.TurnStart;
            Debug.Log($"===== Turn {turnCount} Start =====");

            int rolledEnergy = diceManager.RollDice();
            Debug.Log($"Turn Energy: {rolledEnergy}");

            // 🔥 여기에 준하님의 카드 뽑기 스위치를 켭니다! 🔥
            // (주의: CardManager나 DrawCards 이름은 준하님이 실제 작성하신 스크립트/함수 이름으로 맞춰주세요)
            FindFirstObjectByType<DataManager>().TriggerCardDraw(); 

            // 서버에서 카드를 받아와서 예쁘게 깔릴 때까지 1.5초 정도 충분히 기다려 줍니다.
            yield return new WaitForSeconds(1.5f); 

            gameManager.currentState = BattleState.PlayerTurn;
            playerActionFinished = false;
            Debug.Log("Player Turn Start");

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.PlayerTurn;
            playerActionFinished = false;
            Debug.Log("Player Turn Start");

            while (!playerActionFinished)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (diceManager.HasEnoughEnergy(1))
                    {
                        diceManager.UseEnergy(1);
                        player.Attack(enemy);
                        gameManager.CheckBattleResult();

                        if (ShowBattleResultIfGameOver())
                        {
                            yield break;
                        }
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

            if (ShowBattleResultIfGameOver())
            {
                yield break;
            }

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.TurnEnd;
            Debug.Log($"===== Turn {turnCount} End =====");

            yield return new WaitForSeconds(1f);
        }
    }

    private bool ShowBattleResultIfGameOver()
    {
        if (!gameManager.isGameOver)
        {
            return false;
        }

        if (gameManager.currentState == BattleState.Victory)
        {
            uiManager.ShowResult(true);
        }
        else if (gameManager.currentState == BattleState.Defeat)
        {
            uiManager.ShowResult(false);
        }

        return true;
    }

    public void EndPlayerTurn()
    {
        playerActionFinished = true;
    }
}