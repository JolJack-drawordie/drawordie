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

        if (!DeckManager.Instance.IsDeckInitialized)
        {
            DeckManager.Instance.InitializeDeck(DataManager.Instance.defaultAdjectiveIds, DataManager.Instance.defaultGerundIds);
            DeckManager.Instance.IsDeckInitialized = true; // "이제 초기화 끝났다"고 체크 박아둠
        }
        // 배경 음악
        if (SoundManager.Instance != null && SoundManager.Instance.battleBackgroundSound != null) {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.battleBackgroundSound);
        }

        gameManager.StartBattle();
        if (uiManager != null) uiManager.HideResult();
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        UIManager.Instance.LinkUnitToUI(player);
        UIManager.Instance.LinkUnitToUI(enemy);
        while (!gameManager.isGameOver)
        {
            turnCount++;
            gameManager.currentState = BattleState.TurnStart;
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