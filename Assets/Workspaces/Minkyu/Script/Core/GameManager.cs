using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("전투 유닛")]
    public PlayerUnit player;
    public EnemyUnit enemy;

    [Header("상태")]
    public BattleState currentState = BattleState.None;
    public bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartBattle()
    {
        isGameOver = false;
        currentState = BattleState.BattleStart;
        Debug.Log("전투 시작!");
    }

    public void CheckBattleResult()
    {
        if (enemy != null && enemy.IsDead())
        {
            currentState = BattleState.Victory;
            isGameOver = true;
            Debug.Log("승리!");
        }
        else if (player != null && player.IsDead())
        {
            currentState = BattleState.Defeat;
            isGameOver = true;
            Debug.Log("패배...");
        }
    }
}