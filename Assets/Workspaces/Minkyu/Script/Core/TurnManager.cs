using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("참조")]
    public GameManager gameManager;
    public DiceManager diceManager;
    public PlayerUnit player;
    public EnemyUnit enemy;

    [Header("턴 정보")]
    public int turnCount = 0;
    public bool playerActionFinished = false;

    private void Start()
    {
        gameManager.StartBattle();
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        while (!gameManager.isGameOver)
        {
            // 턴 시작
            turnCount++;
            gameManager.currentState = BattleState.TurnStart;
            Debug.Log($"===== 턴 {turnCount} 시작 =====");

            int rolledEnergy = diceManager.RollDice();
            Debug.Log($"이번 턴 플레이어 에너지: {rolledEnergy}");

            yield return new WaitForSeconds(1f);

            // 플레이어 턴
            gameManager.currentState = BattleState.PlayerTurn;
            playerActionFinished = false;
            Debug.Log("플레이어 턴 시작");

            // 임시 테스트용: 스페이스 누르면 공격 후 턴 종료
            while (!playerActionFinished)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // 예시로 에너지 1 소모 공격
                    if (diceManager.HasEnoughEnergy(1))
                    {
                        diceManager.UseEnergy(1);
                        player.Attack(enemy);
                        gameManager.CheckBattleResult();

                        if (gameManager.isGameOver)
                            yield break;
                    }

                    playerActionFinished = true;
                }

                yield return null;
            }

            yield return new WaitForSeconds(1f);

            // 적 턴
            gameManager.currentState = BattleState.EnemyTurn;
            Debug.Log("적 턴 시작");

            enemy.Attack(player);
            gameManager.CheckBattleResult();

            if (gameManager.isGameOver)
                yield break;

            yield return new WaitForSeconds(1f);

            // 턴 종료
            gameManager.currentState = BattleState.TurnEnd;
            Debug.Log($"===== 턴 {turnCount} 종료 =====");

            yield return new WaitForSeconds(1f);
        }
    }
    public void EndPlayerTurn()
    {
        playerActionFinished = true;
    }
}
