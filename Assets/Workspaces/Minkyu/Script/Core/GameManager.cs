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

    public PlayerUnit currentPlayer;
    public EnemyUnit currentEnemy;

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

        // 새 전투에서만 사냥꾼의 송곳니 효과 초기화
        if (!PendingLoadData.isPending &&
            RelicManager.Instance != null)
        {
            RelicManager.Instance.ResetHunterFangForBattle();
        }

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

            // 새 전투는 방어도 0에서 시작 (플레이어 턴에 적을 처치하면 방어도가 리셋되지 않은 채 다음 전투로 넘어옴)
            // 전투 시작 유물이 주는 방어도는 유지되도록 유물 발동보다 먼저 초기화
            if (!PendingLoadData.isPending && currentPlayer != null)
            {
                currentPlayer.RestoreState(currentPlayer.statData.currentHp, 0);
            }

            // 새 전투에서만 전투 시작 유물 발동
            // 저장된 전투를 불러오는 경우에는 중복 지급하지 않음
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
            // 연출 중이던 다른 카드가 끝나며 다시 호출될 수 있으므로 처음 승리했을 때만 집계
            if (!isGameOver && GameFlowData.currentNodeType == MapNode.NodeType.Elite)
                GameFlowData.eliteKills++;

            // 최초 승리 시에만 유물 효과 처리
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

            // 최종 Act 보스 처치 시 플레이 타임 측정 종료 및 랭킹 등록
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

            // 사망 시 플레이 타임 측정 종료 및 랭킹 등록
            if (GameResultManager.Instance != null)
                GameResultManager.Instance.EndRun(false);

            UIManager.Instance.ShowResult(false);
        }
    }
}