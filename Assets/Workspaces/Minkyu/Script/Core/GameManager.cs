using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("상태")]
    public BattleState currentState = BattleState.None;
    public bool isGameOver = false;

    [Header("스폰 위치")]
    public GameObject playerSpawnPoint;
    public GameObject enemySpawnPoint;

    private PlayerUnit currentPlayer;
    private EnemyUnit currentEnemy;

    public PlayerUnit Player => currentPlayer;
    public EnemyUnit Enemy => currentEnemy;

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

        if (BattleFactory.Instance != null)
        {
            // ⭐️ 팩토리가 생성해 준 오브젝트들을 받아와서 즉시 컴포넌트 추출 및 저장
            var (playerObj, enemyObj) = BattleFactory.Instance.SpawnAll(playerSpawnPoint, enemySpawnPoint);

            if (playerObj != null)
                currentPlayer = playerObj.GetComponent<PlayerUnit>();

            if (enemyObj != null)
                currentEnemy = enemyObj.GetComponent<EnemyUnit>();

            Debug.Log($"생성 완료 - Player: {currentPlayer}, Enemy: {currentEnemy}");
        }
        else
        {
            Debug.LogError("씬에 BattleFactory가 없습니다!");
        }
    }

    // ⭐️ 유닛들이 태어날 때 알아서 여기로 찾아와서 등록함!
    //public void RegisterPlayer(PlayerUnit player)
    //{
    //    currentPlayer = player;
    //    Debug.Log("플레이어 등록 완료");
    //}

    //public void RegisterEnemy(EnemyUnit enemy)
    //{
    //    currentEnemy = enemy;
    //    Debug.Log("적 등록 완료");
    //}

    public void CheckBattleResult()
    {
        if (currentEnemy != null && currentEnemy.IsDead())
        {
            currentState = BattleState.Victory;
            isGameOver = true;
            Debug.Log("승리!");
        }
        else if (currentPlayer != null && currentPlayer.IsDead())
        {
            currentState = BattleState.Defeat;
            isGameOver = true;
            Debug.Log("패배...");
        }
    }
}