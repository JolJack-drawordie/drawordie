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

    [Header("전투 애니메이션 (선택)")]
    public PlayerController playerController;
    public EnemyController enemyController;

    [Header("턴 정보")]
    public int turnCount = 0;
    public bool playerActionFinished = false;

    private bool isDeckInitialized = false;

    private void Start()
    {

        if (DataManager.Instance.isDataLoaded)
        {
            StartGame();
        }
        else
        {
            DataManager.Instance.OnDataLoaded += StartGame;
        }
    }

    private void StartGame()
    {
        DataManager.Instance.OnDataLoaded -= StartGame;

        // ⭐ [로드 기능] 조건 추가 (원래는 !DeckManager.Instance.IsDeckInitialized 만 있었음)
        // 로드된 게임이면 저장된 덱으로 복원되므로 기본 덱 초기화는 건너뜀 (GameManager.StartBattle에서 처리)
        if (!PendingLoadData.isPending && !DeckManager.Instance.IsDeckInitialized)
        {
            DeckManager.Instance.InitializeDeck(DataManager.Instance.defaultAdjectiveIds, DataManager.Instance.defaultGerundIds);
            DeckManager.Instance.IsDeckInitialized = true; // "이제 초기화 끝났다"고 체크 박아둠
        }
        // 배경 음악
        if (SoundManager.Instance != null && SoundManager.Instance.battleBackgroundSound != null) {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.battleBackgroundSound);
        }

        gameManager.StartBattle();

        if (GameManager.Instance != null)
        {
            player = GameManager.Instance.Player;
            enemy = GameManager.Instance.Enemy;
            playerController = PlayerController.Instance;

            if (enemy != null)
            {
                enemyController = enemy.GetComponent<EnemyController>();
            }
        }

        if (uiManager != null) uiManager.HideResult();
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        bool isFirstLoop = true; // ⭐ [로드 기능] 추가

        while (!gameManager.isGameOver)
        {
            turnCount++;
            gameManager.currentState = BattleState.TurnStart;

            // ⭐ [로드 기능] 추가 시작
            // 로드된 게임의 첫 턴: 저장된 손패/코스트가 이미 있으므로 버리기/주사위/드로우를 건너뛰고 그대로 이어서 진행
            bool isResumedTurn = isFirstLoop && PendingLoadData.isPending;
            isFirstLoop = false;

            if (isResumedTurn)
            {
                diceManager.SetCurrentEnergy(PendingLoadData.cost);

                if (DataManager.Instance != null)
                {
                    DataManager.Instance.RestoreHand(DeckManager.Instance.Hand);
                }

                PendingLoadData.Clear();
            }
            // ⭐ [로드 기능] 추가 끝
            else
            {
                DeckManager.Instance.DiscardHand();

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
            }

            gameManager.currentState = BattleState.PlayerTurn;
            Debug.Log("Player Turn Start - 카드를 드래그해 공격하고 턴 종료 버튼을 누르세요.");
            playerActionFinished = false;

            // 카드 드래그로 공격, EndTurnButton이 EndPlayerTurn() 호출할 때까지 대기
            while (!playerActionFinished)
            {
                if (gameManager.isGameOver) yield break;
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.EnemyTurn;
            Debug.Log("Enemy Turn Start");

            if (enemyController != null)
                yield return StartCoroutine(enemyController.PlayAttackAnimation());
            enemy.Attack(player);
            gameManager.CheckBattleResult();

            if (ShowBattleResultIfGameOver()) yield break;

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.TurnEnd;

            //방어도 리셋
            player.ResetShield();
            enemy.ResetShield();

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
        if (DataManager.Instance != null)
            DataManager.Instance.StartDiscardAll();
        playerActionFinished = true;
    }
}