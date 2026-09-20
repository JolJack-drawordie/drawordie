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

        // ⭐ [로드 기능] 추가 시작
        if (PendingLoadData.isPending)
        {
            // 로드된 게임: 저장된 덱 상태(순서 포함)를 그대로 복원, 새로 섞지 않음
            DeckManager.Instance.LoadDeckState(PendingLoadData.deckDataJson);
        }
        else
        {
            DeckManager.Instance.ShuffleDeck();
        }
        // ⭐ [로드 기능] 추가 끝

        if (BattleFactory.Instance != null)
        {
            // ⭐️ 팩토리가 생성해 준 오브젝트들을 받아와서 즉시 컴포넌트 추출 및 저장
            var (playerObj, enemyObj) = BattleFactory.Instance.SpawnAll(playerSpawnPoint, enemySpawnPoint);

            if (playerObj != null)
                currentPlayer = playerObj.GetComponent<PlayerUnit>();

            if (enemyObj != null)
                currentEnemy = enemyObj.GetComponent<EnemyUnit>();

            Debug.Log($"생성 완료 - Player: {currentPlayer}, Enemy: {currentEnemy}");

            // ⭐ [로드 기능] 추가
            if (PendingLoadData.isPending)
            {
                RestoreLoadedUnitState();
            }
        }
        else
        {
            Debug.LogError("씬에 BattleFactory가 없습니다!");
        }
    }

    // ⭐ [로드 기능] 신규 메서드
    // 로드된 게임: 플레이어/몬스터의 저장된 체력·쉴드로 덮어씀 (몬스터 종류/최대치는 nodeSeed로 이미 동일하게 재생성됨)
    private void RestoreLoadedUnitState()
    {
        if (currentPlayer != null)
        {
            currentPlayer.RestoreState(PendingLoadData.hp, PendingLoadData.shield);
        }

        if (currentEnemy != null && !string.IsNullOrEmpty(PendingLoadData.monsterDataJson))
        {
            MonsterSaveData monsterSave = JsonUtility.FromJson<MonsterSaveData>(PendingLoadData.monsterDataJson);
            currentEnemy.RestoreState(monsterSave.currentHp, monsterSave.currentShield);
        }
    }

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