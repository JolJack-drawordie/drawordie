using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private const string SaveUrl = "http://localhost:8080/api/game/save";

    // 씬에 미리 배치하지 않아도 자동으로 생성되도록 함 (MonsterDatabase / StatManager와 동일한 방식)
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

    public void SaveGame()
    {
        if (!AuthManager.isLoggedIn)
        {
            Debug.LogWarning("[SaveManager] 로그인되어 있지 않아 저장할 수 없습니다.");
            return;
        }

        StartCoroutine(SaveGameRoutine());
    }

    private IEnumerator SaveGameRoutine()
    {
        // -----------------------------
        // 턴 수
        // -----------------------------
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        int turnCount = turnManager != null ? turnManager.turnCount : 0;

        // -----------------------------
        // 플레이어 체력 / 실드
        // -----------------------------
        PlayerUnit player =
            GameManager.Instance != null ? GameManager.Instance.Player : null;

        int playerCurrentHp = 0;
        int playerCurrentShield = 0;
        if (player != null && player.statData != null)
        {
            playerCurrentHp = player.statData.currentHp;
            playerCurrentShield = player.statData.currentShield;
        }

        // -----------------------------
        // 코스트 (주사위로 굴린 현재 에너지)
        // -----------------------------
        int currentCost =
            DiceManager.Instance != null ? DiceManager.Instance.CurrentEnergy : 0;

        // -----------------------------
        // 덱 정보
        // -----------------------------
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

        string deckDataJson = JsonUtility.ToJson(deck);

        // -----------------------------
        // 몬스터 정보 (전투 중이 아니면 빈 문자열)
        // -----------------------------
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

            monsterDataJson = JsonUtility.ToJson(monster);
        }

        // -----------------------------
        // 서버로 전송
        // (Seed는 MasterSeedManager / MapSeedGenerator / MapGenerator가 생성한 값)
        // -----------------------------
        WWWForm form = new WWWForm();
        form.AddField("userId", AuthManager.userId.ToString());
        form.AddField("masterSeed", GameFlowData.masterSeed);
        form.AddField("mapSeed", GameFlowData.mapSeed);
        form.AddField("nodeSeed", GameFlowData.currentNodeSeed);
        form.AddField("turnCount", turnCount);
        form.AddField("hp", playerCurrentHp);
        form.AddField("shield", playerCurrentShield);
        form.AddField("cost", currentCost);
        form.AddField("deckData", deckDataJson);
        form.AddField("monsterData", monsterDataJson);
        form.AddField("currentFloor", GameFlowData.currentFloor);
        form.AddField("currentIndex", GameFlowData.currentIndex);

        using (UnityWebRequest www = UnityWebRequest.Post(SaveUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=green>[SaveManager] 서버에 저장 완료!</color>");
            }
            else
            {
                Debug.LogError("[SaveManager] 저장 실패: " + www.error);
            }
        }
    }

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
