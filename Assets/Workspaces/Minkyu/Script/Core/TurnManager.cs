using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 전투의 턴 진행: 주사위 굴리기 → 카드 뽑기 → 플레이어 턴(카드 사용, 턴 종료 버튼) → 적 턴 → 반복.
// 불러온 전투의 첫 턴은 저장된 손패 / 에너지로 바로 이어서 진행한다.
public class TurnManager : MonoBehaviour
{
    [Header("참조")]
    public GameManager gameManager;
    public DiceManager diceManager;
    public UIManager uiManager;
    public PlayerUnit player;
    public EnemyUnit enemy;

    // 적 행동 연출 (전투가 시작되면 적 오브젝트에서 찾음)
    [Header("전투 애니메이션 (선택)")]
    public EnemyController enemyController;

    [Header("턴 종료 버튼")]
    public Button endTurnButton; // 비어 있으면 씬에서 "EndTurnButton" 이름으로 찾음
    public bool alignEndTurnWithRollButton = true; // 주사위 버튼과 같은 자리에 배치

    [Header("턴 정보")]
    public int turnCount = 0;
    public bool playerActionFinished = false; // 턴 종료 버튼을 눌렀는지

    // 전투 씬(턴을 진행하는 씬)이 열려 있는지.
    // 휴식 씬 등에 GameManager가 따라 들어가도 전투로 착각하지 않도록 저장 가능 조건에서 이 값으로 판단한다
    public static bool IsInBattleScene => instance != null;
    private static TurnManager instance;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // 카드 데이터를 서버에서 다 받은 뒤 전투 시작
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

    // 덱 준비 → 전투 BGM → 전투 시작 → 턴 진행
    private void StartGame()
    {
        CardDataManager.Instance.OnDataLoaded -= StartGame;

        // 휴식 씬 세이브를 불러온 경우: 저장된 덱을 복원 (IsDeckInitialized = true가 되어 기본 덱 초기화를 건너뜀)
        if (PendingLoadData.pendingDeckJson != null)
        {
            DeckManager.Instance.LoadDeckState(PendingLoadData.pendingDeckJson);
            PendingLoadData.pendingDeckJson = null;
        }

        // 처음 하는 전투면 기본 덱으로 초기화 (불러온 전투는 GameManager.StartBattle에서 저장된 덱으로 복원)
        if (!PendingLoadData.isPending && !DeckManager.Instance.IsDeckInitialized)
        {
            DeckManager.Instance.InitializeDeck(CardDataManager.Instance.defaultAdjectiveIds, CardDataManager.Instance.defaultGerundIds);
            DeckManager.Instance.IsDeckInitialized = true;
        }
        // 전투 배경음
        if (SoundManager.Instance != null) {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.GetBattleBGM());
        }

        BattleLog.Clear();
        bool isLoadedBattle = PendingLoadData.isPending;

        gameManager.StartBattle();

        if (GameManager.Instance != null)
        {
            player = GameManager.Instance.Player;
            enemy = GameManager.Instance.Enemy;

            BattleLog.AddHeader(isLoadedBattle ? "전투 이어서 진행" : "전투 시작");

            if (enemy != null)
            {
                enemyController = enemy.GetComponent<EnemyController>();
            }
        }

        if (uiManager != null) uiManager.HideResult();
        StartCoroutine(BattleLoop());
    }

    // 전투가 끝날 때까지 턴을 반복
    private IEnumerator BattleLoop()
    {
        bool isFirstLoop = true; // 불러온 전투의 첫 턴인지 판단용

        while (!gameManager.isGameOver)
        {
            turnCount++;
            gameManager.currentState = BattleState.TurnStart;
            BattleLog.AddTurn(turnCount);

            // 불러온 전투의 첫 턴: 저장된 손패 / 에너지가 이미 있으므로 버리기 / 주사위 / 뽑기를 건너뛰고 그대로 이어서 진행
            bool isResumedTurn = isFirstLoop && PendingLoadData.isPending;

            // 세이브에 있던 손패의 조합 카드 (첫 턴에만 쓰고 비움)
            List<ComboSaveData> savedCombos = null;
            if (isFirstLoop)
            {
                if (isResumedTurn) savedCombos = DeckManager.Instance.LoadedComboHand;
                DeckManager.Instance.LoadedComboHand = null;
            }
            bool hasSavedCombos = savedCombos != null && savedCombos.Count > 0;

            isFirstLoop = false;

            // 주사위를 굴리기 전(손패를 버렸지만 아직 새로 뽑지 않은 상태)에 저장했으면
            // 복원할 손패가 없으므로 새 턴처럼 주사위 굴리기부터 다시 시작한다
            if (isResumedTurn && DeckManager.Instance.Hand.Count == 0 && !hasSavedCombos)
            {
                isResumedTurn = false;
            }

            if (isResumedTurn)
            {
                diceManager.SetCurrentEnergy(PendingLoadData.cost, PendingLoadData.maxCost);

                if (DataManager.Instance != null)
                {
                    DataManager.Instance.RestoreHand(DeckManager.Instance.Hand);

                    // 조합 카드는 조합표를 받아 온 뒤에 다시 만든다
                    if (hasSavedCombos)
                    {
                        yield return new WaitUntil(() => ComboManager.Instance == null || ComboManager.Instance.IsLoadFinished);
                        DataManager.Instance.RestoreComboCards(savedCombos);
                    }
                }
            }
            // 새 턴: 손패 버리기 → 주사위 → 카드 뽑기
            else
            {
                DeckManager.Instance.DiscardHand();

                // 주사위 굴리기 버튼을 누를 때까지 대기
                diceManager.ShowRollButton();
                yield return new WaitUntil(() => diceManager.isRollFinished);

                int rolledEnergy = diceManager.CurrentEnergy;
                Debug.Log($"Turn Energy: {rolledEnergy}");
                BattleLog.Add($"주사위 {diceManager.diceValue} → 에너지 {rolledEnergy - diceManager.BonusEnergy}", BattleLog.Kind.Dice);
                foreach (var bonus in diceManager.BonusSources)
                    BattleLog.Add($"{bonus.source} 발동! 에너지 +{bonus.amount}", BattleLog.Kind.Dice);

                // 이번 턴 손패 뽑기
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.TriggerCardDraw();
                }

                yield return new WaitForSeconds(1.5f);
            }

            if (PendingLoadData.isPending) PendingLoadData.Clear();

            gameManager.currentState = BattleState.PlayerTurn;
            Debug.Log("Player Turn Start - 카드를 드래그해 공격하고 턴 종료 버튼을 누르세요.");
            playerActionFinished = false;
            ShowEndTurnButton(true);

            // 카드를 끌어 사용하다가 턴 종료 버튼(EndPlayerTurn)을 누를 때까지 대기
            while (!playerActionFinished)
            {
                if (gameManager.isGameOver)
                {
                    ShowEndTurnButton(false);
                    yield break;
                }

                // 카드 연출 중에는 턴 종료 버튼을 누를 수 없게
                if (endTurnButton != null)
                    endTurnButton.interactable = !EnemyTarget.IsActing;

                yield return null;
            }

            // 유물 '응축된 마력석': 턴 종료 버튼을 누르자마자 남은 에너지만큼 방어도 (적 공격 전에 받아야 의미가 있음)
            if (RelicManager.Instance != null)
            {
                RelicManager.Instance.OnTurnEnd(diceManager.CurrentEnergy, player);
            }

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.EnemyTurn;
            Debug.Log("Enemy Turn Start");
            BattleLog.AddHeader($"{enemy.LogName}의 차례");

            // 적 방어도는 지난 적 행동에서 얻은 것이 플레이어 턴 동안 유지되다가, 다음 적 행동 직전에 사라진다
            enemy.ResetShield();

            if (enemyController != null)
                yield return StartCoroutine(enemyController.ExecuteAction());
            gameManager.CheckBattleResult();

            if (ShowBattleResultIfGameOver()) yield break;

            // 적의 공격이 끝난 뒤 플레이어 방어도 초기화
            player.ResetShield();

            yield return new WaitForSeconds(1f);

            gameManager.currentState = BattleState.TurnEnd;

            Debug.Log($"===== Turn {turnCount} End =====");

            yield return new WaitForSeconds(1f);
        }
    }

    // 턴 종료 버튼 연결 (주사위 버튼과 같은 자리에 두고, 상황에 따라 둘 중 하나만 보이게)
    private void SetupEndTurnButton()
    {
        if (endTurnButton == null)
        {
            GameObject found = GameObject.Find("EndTurnButton");
            if (found != null) endTurnButton = found.GetComponent<Button>();
        }
        if (endTurnButton == null) return;

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

    // 전투가 끝났으면 결과 화면을 띄우고 true
    private bool ShowBattleResultIfGameOver()
    {
        if (!gameManager.isGameOver) return false;

        if (gameManager.currentState == BattleState.Victory)
            uiManager.ShowResult(true);
        else if (gameManager.currentState == BattleState.Defeat)
            uiManager.ShowResult(false);

        return true;
    }

    // 턴 종료 버튼 (onClick에 연결)
    public void EndPlayerTurn()
    {
        // 플레이어 턴이 아니거나 이미 종료를 눌렀으면 무시
        if (playerActionFinished || gameManager.currentState != BattleState.PlayerTurn) return;

        // 카드 연출 중에는 턴 종료 불가
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