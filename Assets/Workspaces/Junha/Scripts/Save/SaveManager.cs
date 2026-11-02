using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// 지금 진행 상황(플레이어, 덱, 적, 유물, 맵 위치, 플레이 타임)을 서버에 저장한다. 첫 씬 로드 전에 자동으로 만들어진다.
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private const string SaveUrl = "http://localhost:8080/api/game/save";

    private const string MapSceneName = "MapScene";
    private const string WorldMapSceneName = "WorldMapScene";

    // 씬에 미리 배치하지 않아도 게임 시작 시 자동으로 만들어진다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        if (Instance != null) return;

        GameObject obj = new GameObject("SaveManager");
        obj.AddComponent<SaveManager>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 서버 응답을 기다리는 중인지 (저장 버튼 연타 방지)
    public bool IsSaving { get; private set; }

    // 지금 저장해도 되는 시점인지. 전투 씬이면 플레이어 턴이고 카드 연출 중이 아닐 때만 (도중에 저장하면 어중간한 상태가 저장됨)
    // 저장 버튼과 설정 창의 '로비로' 버튼이 같이 쓴다
    public static bool IsSafeToSave()
    {
        // 전투 씬이 아니면(휴식 씬 등) 언제든 저장 가능.
        // GameManager 존재 여부로 판단하지 않음: 다른 씬에 따라 들어온 GameManager를 전투로 착각해 저장이 막히기 때문
        if (!TurnManager.IsInBattleScene) return true;

        // 전투 씬인데 아직 전투 준비(덱 / 적)가 끝나지 않았으면 저장하지 않음
        GameManager game = GameManager.Instance;
        if (game == null) return false;

        return !game.isGameOver && game.currentState == BattleState.PlayerTurn && !EnemyTarget.IsActing;
    }

    // 지금 씬이 저장을 지원하는지. 맵 / 월드맵 씬은 불러왔을 때 돌아갈 곳이 없어서(LoadManager는 휴식 / 전투 씬으로만 이동) 저장하지 않는다
    public static bool IsSaveSupportedScene()
    {
        string scene = SceneManager.GetActiveScene().name;
        return scene != MapSceneName && scene != WorldMapSceneName;
    }

    // 저장 요청. onComplete: 저장 성공 여부 (저장이 끝난 뒤 씬을 옮길 때 사용)
    public void SaveGame(Action<bool> onComplete = null)
    {
        if (!AuthManager.isLoggedIn)
        {
            Debug.LogWarning("[SaveManager] 로그인되어 있지 않아 저장할 수 없습니다.");
            onComplete?.Invoke(false);
            return;
        }

        if (!IsSaveSupportedScene())
        {
            Debug.LogWarning("[SaveManager] 맵 / 월드맵 씬에서는 저장할 수 없습니다.");
            onComplete?.Invoke(false);
            return;
        }

        // 게임이 끝난 뒤에는 저장하지 않음 (결과를 등록하면 서버가 세이브를 지움)
        if (PlayTimeTracker.Instance != null && PlayTimeTracker.Instance.IsRunEnded)
        {
            Debug.LogWarning("[SaveManager] 이미 끝난 게임은 저장할 수 없습니다.");
            onComplete?.Invoke(false);
            return;
        }

        if (IsSaving)
        {
            Debug.LogWarning("[SaveManager] 이미 저장 중입니다.");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(SaveGameRoutine(onComplete));
    }

    // 저장할 값을 모아 서버로 보낸다
    private IEnumerator SaveGameRoutine(Action<bool> onComplete)
    {
        // ----- 플레이어 체력 / 방어도 -----
        PlayerUnit player =
            GameManager.Instance != null ? GameManager.Instance.Player : null;

        int playerCurrentHp = 0;
        int playerMaxHp = 0;
        int playerCurrentShield = 0;
        if (player != null && player.statData != null)
        {
            playerCurrentHp = player.statData.currentHp;
            playerMaxHp = player.statData.maxHp;
            playerCurrentShield = player.statData.currentShield;
        }
        else if (StatManager.Instance != null && StatManager.Instance.runtimePlayerStat != null)
        {
            // 전투 밖(휴식 씬 등): 씬 사이에서 유지되는 플레이어 스탯 사용
            playerCurrentHp = StatManager.Instance.runtimePlayerStat.currentHp;
            playerMaxHp = StatManager.Instance.runtimePlayerStat.maxHp;
            playerCurrentShield = 0;
        }

        // ----- 에너지 (이번 턴 주사위로 얻은 값) -----
        int currentCost =
            DiceManager.Instance != null ? DiceManager.Instance.CurrentEnergy : 0;
        int maxCost =
            DiceManager.Instance != null ? DiceManager.Instance.MaxEnergy : 0;

        // ----- 덱 -----
        DeckSaveData deck = new DeckSaveData();

        if (DeckManager.Instance != null)
        {
            FillCardList(deck.adjectiveDrawPile, DeckManager.Instance.AdjectiveDrawPile);
            FillCardList(deck.adjectiveDiscardPile, DeckManager.Instance.AdjectiveDiscardPile);
            FillCardList(deck.gerundDrawPile, DeckManager.Instance.GerundDrawPile);
            FillCardList(deck.gerundDiscardPile, DeckManager.Instance.GerundDiscardPile);
            FillCardList(deck.hand, DeckManager.Instance.Hand);

            if (DeckManager.Instance.AdjectiveSlot != null)
            {
                deck.hasAdjectiveSlotCard = true;
                deck.adjectiveSlotCard = ToCardSaveData(DeckManager.Instance.AdjectiveSlot);
            }

            if (DeckManager.Instance.GerundSlot != null)
            {
                deck.hasGerundSlotCard = true;
                deck.gerundSlotCard = ToCardSaveData(DeckManager.Instance.GerundSlot);
            }
        }

        // 조합 카드는 DeckManager가 아니라 손패 UI에만 있음
        if (DataManager.Instance != null)
            deck.comboHand = DataManager.Instance.GetComboCardsInHand();

        string deckDataJson = JsonUtility.ToJson(deck);

        // 휴식 씬 세이브를 불러온 뒤 아직 전투에서 덱을 복원하지 않았다면, 불러온 덱을 그대로 다시 저장
        if (PendingLoadData.pendingDeckJson != null)
            deckDataJson = PendingLoadData.pendingDeckJson;

        // ----- 적 (전투 중이 아니면 빈 문자열) -----
        EnemyUnit enemy =
            GameManager.Instance != null ? GameManager.Instance.Enemy : null;

        string monsterDataJson = "";
        if (enemy != null && enemy.statData != null)
        {
            MonsterSaveData monster = new MonsterSaveData
            {
                unitName = enemy.statData.unitName,
                currentHp = enemy.statData.currentHp,
                maxHp = enemy.statData.maxHp,
                currentShield = enemy.statData.currentShield,
                maxShield = enemy.statData.maxShield
            };

            // 다음 행동 / 보스 기믹 진행 상태
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
                enemyController.WriteSaveState(monster);

            monsterDataJson = JsonUtility.ToJson(monster);
        }

        // ----- 보유 유물 (key 목록) -----
        RelicSaveData relics = new RelicSaveData { relicKeys = RelicManager.GetOwnedRelicKeys() };
        string relicDataJson = JsonUtility.ToJson(relics);

        // ----- 서버로 전송 (시드는 MasterSeedManager / MapSeedGenerator / MapGenerator가 만든 값) -----
        WWWForm form = new WWWForm();
        form.AddField("masterSeed", GameFlowData.masterSeed);
        form.AddField("mapSeed", GameFlowData.mapSeed);
        form.AddField("nodeSeed", GameFlowData.currentNodeSeed);
        form.AddField("hp", playerCurrentHp);
        form.AddField("maxHp", playerMaxHp);
        form.AddField("shield", playerCurrentShield);
        form.AddField("cost", currentCost);
        form.AddField("maxCost", maxCost);
        form.AddField("deckData", deckDataJson);
        form.AddField("monsterData", monsterDataJson);
        form.AddField("relicData", relicDataJson);
        form.AddField("currentFloor", GameFlowData.currentFloor);
        form.AddField("currentIndex", GameFlowData.currentIndex);
        form.AddField("act", GameFlowData.currentAct);
        form.AddField("nodeType", (int)GameFlowData.currentNodeType);
        form.AddField("rested", GameFlowData.hasRested ? "true" : "false");
        form.AddField("playTime",
            PlayTimeTracker.Instance != null ? PlayTimeTracker.Instance.GetElapsedSecondsInt() : 0);
        form.AddField("eliteKills", GameFlowData.eliteKills);

        IsSaving = true;
        using (UnityWebRequest www = UnityWebRequest.Post(SaveUrl, form))
        {
            // 유저 번호는 서버가 토큰에서 꺼내 쓴다
            AuthManager.SetAuthHeader(www);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=green>[SaveManager] 서버에 저장 완료!</color>");
            }
            else if (www.responseCode == 401)
            {
                Debug.LogError("[SaveManager] 저장 실패: 로그인이 만료되었습니다. 다시 로그인해 주세요.");
            }
            else
            {
                Debug.LogError("[SaveManager] 저장 실패: " + www.error);
            }

            IsSaving = false;
            onComplete?.Invoke(www.result == UnityWebRequest.Result.Success);
        }
    }

    // 카드 목록 → 저장용 (id + 종류) 목록
    private void FillCardList(List<CardSaveData> target, List<ICard> source)
    {
        target.Clear();

        foreach (ICard card in source)
        {
            target.Add(ToCardSaveData(card));
        }
    }

    private CardSaveData ToCardSaveData(ICard card)
    {
        return new CardSaveData(card.Id, card.Type);
    }
}
