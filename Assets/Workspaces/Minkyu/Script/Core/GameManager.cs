using UnityEngine;

// 전투 씬의 상태(진행 단계, 승패)와 플레이어 / 적 유닛을 관리한다.
// 전투 시작 시 덱 준비 → 유닛 스폰(BattleFactory) → 세이브 복원 / 전투 시작 유물을 처리하고, 승패를 판정한다.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("상태")]
    public BattleState currentState = BattleState.None;
    public bool isGameOver = false;

    [Header("스폰 위치")]
    public GameObject playerSpawnPoint;
    public GameObject enemySpawnPoint;

    public PlayerUnit currentPlayer;
    public EnemyUnit currentEnemy;

    // 다른 스크립트가 쓰는 짧은 이름
    public PlayerUnit Player => currentPlayer;
    public EnemyUnit Enemy => currentEnemy;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // TurnManager가 전투를 시작할 때 호출
    public void StartBattle()
    {
        isGameOver = false;
        currentState = BattleState.BattleStart;
        Debug.Log("전투 시작!");

        // 유물 '사냥꾼의 송곳니'는 새 전투에서만 다시 쓸 수 있게 (불러온 전투는 저장 당시 상태 유지)
        if (!PendingLoadData.isPending &&
            RelicManager.Instance != null)
        {
            RelicManager.Instance.ResetHunterFangForBattle();
        }

        // 불러온 전투는 저장된 덱 상태(순서 포함)를 그대로 복원하고, 새 전투는 덱을 섞는다
        if (PendingLoadData.isPending)
        {
            DeckManager.Instance.LoadDeckState(PendingLoadData.deckDataJson);
        }
        else
        {
            DeckManager.Instance.ShuffleDeck();
        }

        if (BattleFactory.Instance != null)
        {
            // BattleFactory가 만든 플레이어 / 적 오브젝트에서 유닛 컴포넌트를 꺼내 둔다
            var (playerObj, enemyObj) = BattleFactory.Instance.SpawnAll(playerSpawnPoint, enemySpawnPoint);

            if (playerObj != null)
                currentPlayer = playerObj.GetComponent<PlayerUnit>();

            if (enemyObj != null)
                currentEnemy = enemyObj.GetComponent<EnemyUnit>();

            Debug.Log($"생성 완료 - Player: {currentPlayer}, Enemy: {currentEnemy}");

            if (PendingLoadData.isPending)
            {
                RestoreLoadedUnitState();
            }

            // 새 전투는 방어도 0에서 시작 (플레이어 턴에 적을 처치하면 방어도가 남은 채 다음 전투로 넘어오므로).
            // 전투 시작 유물이 주는 방어도는 남도록 유물 발동보다 먼저 초기화
            if (!PendingLoadData.isPending && currentPlayer != null)
            {
                currentPlayer.RestoreState(currentPlayer.statData.currentHp, 0);
            }

            // 전투 시작 유물은 새 전투에서만 발동 (불러온 전투는 중복 지급하지 않음)
            if (!PendingLoadData.isPending &&
                RelicManager.Instance != null)
            {
                RelicManager.Instance.OnBattleStart(currentPlayer);
            }
        }
        else
        {
            Debug.LogError("씬에 BattleFactory가 없습니다!");
        }
    }

    // 불러온 전투: 플레이어 / 적의 저장된 체력과 방어도로 덮어쓴다 (적 종류와 최대치는 노드 시드로 이미 같게 다시 만들어짐)
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

            // 다음 행동 / 보스 기믹 상태는 적의 Start에서 무작위 대신 이 값으로 복원
            EnemyController enemyController = currentEnemy.GetComponent<EnemyController>();
            if (enemyController != null)
                enemyController.SetSavedState(monsterSave);
        }
    }

    // 승패 판정. 카드 사용 / 적 행동 / 반사 피해 등 체력이 바뀔 때마다 호출된다
    public void CheckBattleResult()
    {
        if (currentEnemy != null && currentEnemy.IsDead())
        {
            // 연출 중이던 다른 카드가 끝나며 다시 호출될 수 있으므로 처음 이겼을 때만 집계
            if (!isGameOver && GameFlowData.currentNodeType == MapNode.NodeType.Elite)
                GameFlowData.eliteKills++;

            // 승리 유물 효과도 처음 이겼을 때만
            bool isFirstVictory = !isGameOver;

            if (isFirstVictory)
            {
                BattleLog.AddHeader($"{currentEnemy.LogName} 처치! 승리");

                if (RelicManager.Instance != null)
                {
                    RelicManager.Instance.OnBattleWon(currentPlayer);
                }
            }

            currentState = BattleState.Victory;
            isGameOver = true;
            Debug.Log("승리!");

            // 마지막 Act 보스를 처치하면 런 종료 (플레이 타임 측정 종료 + 랭킹 등록)
            if (GameFlowData.currentNodeType == MapNode.NodeType.Boss &&
                GameFlowData.IsFinalAct() &&
                GameResultManager.Instance != null)
            {
                GameResultManager.Instance.EndRun(true);
            }
        }
        else if (currentPlayer != null && currentPlayer.IsDead())
        {
            PlayerSpineController.Instance.PlayDefeat();

            if (!isGameOver) BattleLog.AddHeader("플레이어 쓰러짐... 패배");

            currentState = BattleState.Defeat;
            isGameOver = true;

            Debug.Log("패배...");

            // 사망 시에도 런 종료 (플레이 타임 측정 종료 + 랭킹 등록)
            if (GameResultManager.Instance != null)
                GameResultManager.Instance.EndRun(false);

            UIManager.Instance.ShowResult(false);
        }
    }
}