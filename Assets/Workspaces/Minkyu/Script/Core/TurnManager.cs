using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("턴 종료 버튼")]
    public Button endTurnButton; // 비어있으면 씬에서 "EndTurnButton" 이름으로 찾음
    public bool alignEndTurnWithRollButton = true; // 주사위 버튼과 같은 위치에 배치

    [Header("턴 정보")]
    public int turnCount = 0;
    public bool playerActionFinished = false;

    private bool isDeckInitialized = false;

    private void Start()
    {
        SetupEndTurnButton();

        if (CardDataManager.Instance.isDataLoaded)
        {
            StartGame();
        }
        else
        {
            CardDataManager.Instance.OnDataLoaded += StartGame;
        }
    }

    private void StartGame()
    {
        CardDataManager.Instance.OnDataLoaded -= StartGame;

        // 휴식 씬 세이브를 불러온 경우: 저장된 덱을 복원 (IsDeckInitialized = true가 되어 기본 덱 초기화를 건너뜀)
        if (PendingLoadData.pendingDeckJson != null)
        {
            DeckManager.Instance.LoadDeckState(PendingLoadData.pendingDeckJson);
            PendingLoadData.pendingDeckJson = null;
        }

        // ⭐ [로드 기능] 조건 추가 (원래는 !DeckManager.Instance.IsDeckInitialized 만 있었음)
        // 로드된 게임이면 저장된 덱으로 복원되므로 기본 덱 초기화는 건너뜀 (GameManager.StartBattle에서 처리)
        if (!PendingLoadData.isPending && !DeckManager.Instance.IsDeckInitialized)
        {
            DeckManager.Instance.InitializeDeck(CardDataManager.Instance.defaultAdjectiveIds, CardDataManager.Instance.defaultGerundIds);
            DeckManager.Instance.IsDeckInitialized = true; // "이제 초기화 끝났다"고 체크 박아둠
        }
        // 배경 음악
        if (SoundManager.Instance != null) {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.GetBattleBGM());
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

            // 주사위를 굴리기 전(손패를 이미 버렸지만 아직 새로 뽑지 않은 상태)에 저장된 경우
            // 복원할 손패가 없으므로, 이어서 진행하지 않고 새 턴처럼 주사위 굴리기부터 다시 시작한다.
            if (isResumedTurn && DeckManager.Instance.Hand.Count == 0)
            {
                isResumedTurn = false;
            }

            if (isResumedTurn)
            {
                diceManager.SetCurrentEnergy(PendingLoadData.cost, PendingLoadData.maxCost);

                if (DataManager.Instance != null)
                {
                    DataManager.Instance.RestoreHand(DeckManager.Instance.Hand);
                }
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

            if (PendingLoadData.isPending) PendingLoadData.Clear();

            gameManager.currentState = BattleState.PlayerTurn;
            Debug.Log("Player Turn Start - 카드를 드래그해 공격하고 턴 종료 버튼을 누르세요.");
            playerActionFinished = false;
            ShowEndTurnButton(true);

            // 카드 드래그로 공격, EndTurnButton이 EndPlayerTurn() 호출할 때까지 대기
            while (!playerActionFinished)
            {
                if (gameManager.isGameOver)
                {
                    ShowEndTurnButton(false);
                    yield break;
                }

                // 카드 사용(공격 연출) 중에는 회색 처리
                if (endTurnButton != null)
                    endTurnButton.interactable = !EnemyTarget.IsActing;

                yield return null;
            }

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.EnemyTurn;
            Debug.Log("Enemy Turn Start");

            // 적 방어도는 지난 적 행동에서 얻은 것이 플레이어 턴 동안 유지되다가, 다음 적 행동 직전에 사라짐
            enemy.ResetShield();

            if (enemyController != null)
                yield return StartCoroutine(enemyController.PlayAttackAnimation());
            enemy.Attack(player);
            gameManager.CheckBattleResult();

            if (ShowBattleResultIfGameOver()) yield break;

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.TurnEnd;

            // 플레이어 방어도 리셋 (적 방어도는 다음 적 턴 시작 시 리셋)
            player.ResetShield();

            Debug.Log($"===== Turn {turnCount} End =====");

            yield return new WaitForSeconds(1f);
        }
    }

    private void SetupEndTurnButton()
    {
        if (endTurnButton == null)
        {
            GameObject found = GameObject.Find("EndTurnButton");
            if (found != null) endTurnButton = found.GetComponent<Button>();
        }
        if (endTurnButton == null) return;

        // 주사위 버튼과 같은 자리에 겹쳐 두고, 상황에 따라 둘 중 하나만 보이게 함
        if (alignEndTurnWithRollButton && diceManager != null && diceManager.rollDiceButton != null)
            endTurnButton.transform.position = diceManager.rollDiceButton.transform.position;

        ShowEndTurnButton(false);
    }

    private void ShowEndTurnButton(bool show)
    {
        if (endTurnButton == null) return;
        endTurnButton.interactable = true;
        endTurnButton.gameObject.SetActive(show);
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
        // 플레이어 턴이 아니거나 이미 종료 요청됐으면 무시
        if (playerActionFinished || gameManager.currentState != BattleState.PlayerTurn) return;

        // 카드 사용(공격 연출) 중에는 턴 종료 불가
        if (EnemyTarget.IsActing)
        {
            Debug.Log("카드 사용 중에는 턴을 종료할 수 없습니다.");
            return;
        }

        ShowEndTurnButton(false);

        if (DataManager.Instance != null)
            DataManager.Instance.StartDiscardAll();
        playerActionFinished = true;
    }
}