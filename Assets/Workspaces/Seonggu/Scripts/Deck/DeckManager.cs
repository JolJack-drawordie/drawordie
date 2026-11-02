using System.Collections.Generic;
using UnityEngine;

// 덱 데이터 관리: 형용사 / 동명사 각각의 뽑을 더미와 버린 더미, 손패, 조합 슬롯에 올라간 카드.
// 씬이 바뀌어도 유지되어 덱이 다음 전투로 이어진다. 화면의 카드 UI는 DataManager가 따로 그린다.
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    // 덱 섞기용 난수 (노드 시드 기반이라 같은 노드면 같은 순서)
    System.Random shuffleRng;

    public bool IsDeckInitialized { get; set; } = false; // 기본 덱으로 초기화를 마쳤는지 (새 게임이면 다시 false)

    // 형용사 카드 더미
    public List<ICard> AdjectiveDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> AdjectiveDiscardPile { get; private set; } = new List<ICard>();

    // 동명사 카드 더미
    public List<ICard> GerundDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> GerundDiscardPile { get; private set; } = new List<ICard>();

    public List<ICard> Hand { get; private set; } = new List<ICard>(); // 지금 손패 (형용사 / 동명사, 조합 카드는 손패 UI에만 있음)

    public ICard AdjectiveSlot; // 형용사 슬롯에 올라간 카드
    public ICard GerundSlot; // 동명사 슬롯에 올라간 카드

    // 세이브 로드 시 복원할 손패의 조합 카드 (전투 첫 턴에 TurnManager가 손패 UI로 만든 뒤 비움)
    public List<ComboSaveData> LoadedComboHand { get; set; }

    public void Awake()
    {
        // 처음 만들어진 하나만 남기고 씬이 바뀌어도 유지
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

    public void Start()
    {
        int nodeSeed = GameFlowData.currentNodeSeed;

        // 몬스터 / 카드 보상 시드와 겹치지 않도록 덱 섞기 전용 오프셋
        shuffleRng = new System.Random(nodeSeed + 3);
    }

    // 새 게임: 이전 판의 덱을 비우고 다음 전투에서 기본 덱으로 다시 초기화되게 함
    public void ResetForNewRun()
    {
        AdjectiveDrawPile.Clear();
        AdjectiveDiscardPile.Clear();
        GerundDrawPile.Clear();
        GerundDiscardPile.Clear();
        Hand.Clear();
        AdjectiveSlot = null;
        GerundSlot = null;
        LoadedComboHand = null;
        IsDeckInitialized = false;
    }

    // 기본 덱(서버 id 목록)으로 뽑을 더미를 채운다
    public void InitializeDeck(List<int> adjectiveIds, List<int> gerundIds)
    {
        AdjectiveDrawPile.Clear();
        GerundDrawPile.Clear();

        foreach (int id in adjectiveIds)
        {
            var data = CardDataManager.Instance.adjectiveTable[id];
            AdjectiveDrawPile.Add(new AdjectiveCard(data));
        }

        foreach (int id in gerundIds)
        {
            var data = CardDataManager.Instance.gerundTable[id];
            GerundDrawPile.Add(new GerundCard(data));
        }
    }
    // 세이브 로드: 저장된 덱 상태(더미 / 버린 더미 / 손패 / 슬롯)를 그대로 복원
    public void LoadDeckState(string deckDataJson)
    {
        AdjectiveDrawPile.Clear();
        AdjectiveDiscardPile.Clear();
        GerundDrawPile.Clear();
        GerundDiscardPile.Clear();
        Hand.Clear();
        AdjectiveSlot = null;
        GerundSlot = null;
        LoadedComboHand = null;

        if (!string.IsNullOrEmpty(deckDataJson))
        {
            DeckSaveData saved = JsonUtility.FromJson<DeckSaveData>(deckDataJson);

            FillPileFromSave(AdjectiveDrawPile, saved.adjectiveDrawPile);
            FillPileFromSave(AdjectiveDiscardPile, saved.adjectiveDiscardPile);
            FillPileFromSave(GerundDrawPile, saved.gerundDrawPile);
            FillPileFromSave(GerundDiscardPile, saved.gerundDiscardPile);
            FillPileFromSave(Hand, saved.hand);

            // 슬롯에 있던 카드는 슬롯 UI를 복원하지 않고 손패로 되돌려 잃어버리지 않게 함
            if (saved.hasAdjectiveSlotCard && saved.adjectiveSlotCard != null)
                Hand.Add(CreateCardFromSave(saved.adjectiveSlotCard));

            if (saved.hasGerundSlotCard && saved.gerundSlotCard != null)
                Hand.Add(CreateCardFromSave(saved.gerundSlotCard));

            // 예전 세이브에는 없음 (null)
            LoadedComboHand = saved.comboHand;
        }

        IsDeckInitialized = true;
    }

    // 저장된 카드 목록 → 카드 객체 목록
    private void FillPileFromSave(List<ICard> target, List<CardSaveData> source)
    {
        if (source == null) return;

        foreach (CardSaveData cardData in source)
        {
            ICard card = CreateCardFromSave(cardData);
            if (card != null) target.Add(card);
        }
    }

    // 저장된 카드 하나 → 카드 객체 (서버 카드 데이터 표에서 찾음)
    private ICard CreateCardFromSave(CardSaveData cardData)
    {
        if (cardData.type == CardType.Adjective)
        {
            var data = CardDataManager.Instance.adjectiveTable[cardData.id];
            return new AdjectiveCard(data);
        }
        else if (cardData.type == CardType.Gerund)
        {
            var data = CardDataManager.Instance.gerundTable[cardData.id];
            return new GerundCard(data);
        }

        Debug.LogWarning($"[DeckManager] 세이브 카드 타입을 알 수 없음: {cardData.type}");
        return null;
    }

    // 전투 시작 시 뽑을 더미 섞기
    public void ShuffleDeck()
    {
        // 아직 섞기용 난수가 없으면 여기서 만든다
        if (shuffleRng == null)
        {
            int nodeSeed = GameFlowData.currentNodeSeed;
            shuffleRng = new System.Random(nodeSeed + 3);
        }

        Shuffle(AdjectiveDrawPile);
        Shuffle(GerundDrawPile);
    }

    // 피셔-예이츠 섞기
    private void Shuffle(List<ICard> list)
    {
        if (list == null) return;

        for (int i = 0; i < list.Count; i++)
        {
            int rnd = shuffleRng.Next(i, list.Count);
            ICard temp = list[rnd];
            list[rnd] = list[i];
            list[i] = temp;
        }
    }

    // 손패 뽑기: 형용사 2장 + 동명사 3장
    public void DrawHand()
    {
        for (int i = 0; i < 2; i++)
        {
            DrawCard(true);
        }

        for (int i = 0; i < 3; i++)
        {
            DrawCard(false);
        }
    }

    // 한 장 뽑기. 뽑을 더미가 비었으면 버린 더미를 섞어서 다시 채운다
    public void DrawCard(bool isAdjective)
    {
        var drawPile = isAdjective ? AdjectiveDrawPile : GerundDrawPile;
        var discardPile = isAdjective ? AdjectiveDiscardPile : GerundDiscardPile;

        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return; // 둘 다 비었으면 뽑을 카드가 없음

            RefillDeckFromDiscard(isAdjective);
        }

        // 맨 위 카드를 손패로
        if (drawPile.Count > 0)
        {
            ICard card = drawPile[0];
            Hand.Add(card);
            drawPile.RemoveAt(0);

            Debug.Log($"[Draw] {card.Name} (타입: {card.Type}) 뽑음! 남은 카드 수: {drawPile.Count}");
        }
    }

    // 이번 턴 손패를 뽑고, 카드 UI를 만들 수 있게 서버 데이터 형태로 묶어서 돌려준다 (DataManager가 사용)
    public BattleStartHand PrepareInitialHand()
    {
        Hand.Clear();
        DrawHand();

        BattleStartHand handData = new BattleStartHand();
        handData.adjectives = new List<Adjective>();
        handData.gerunds = new List<Gerund>();

        foreach (var card in Hand)
        {
            if (card is AdjectiveCard adjCard)
            {
                handData.adjectives.Add(adjCard.GetData());
            }
            else if (card is GerundCard gerCard)
            {
                handData.gerunds.Add(gerCard.GetData());
            }
        }

        return handData;
    }

    // 보상으로 고른 카드를 뽑을 더미에 추가
    public void AddCardToDeck(ICard chosenCard)
    {
        if (chosenCard == null)
        {
            Debug.LogError("[DeckManager] 추가하려는 카드가 null입니다!");
            return;
        }

        // 종류에 맞는 더미에 추가
        if (chosenCard.Type == CardType.Adjective)
        {
            AdjectiveDrawPile.Add(chosenCard);
            Debug.Log($"[DeckManager] 형용사 덱에 카드 추가 완료: {chosenCard.Name}");
        }
        else if (chosenCard.Type == CardType.Gerund)
        {
            GerundDrawPile.Add(chosenCard);
            Debug.Log($"[DeckManager] 동명사 덱에 카드 추가 완료: {chosenCard.Name}");
        }
        else
        {
            Debug.LogWarning($"[DeckManager] 처리할 수 없는 카드 타입입니다: {chosenCard.Type}");
        }
        DebugShowAllCards();
    }

    // 카드 한 장을 손패에서 빼고 버린 더미로
    public void DiscardCard(ICard card)
    {
        if (card == null)
        {
            Debug.LogError("[DeckManager] 버리려는 카드가 null입니다!");
            return;
        }

        ICard target = Hand.Find(c => c.Id == card.Id && c.Type == card.Type);
        if (target != null)
        {
            Hand.Remove(target);
        }

        if (card.Type == CardType.Adjective)
        {
            AdjectiveDiscardPile.Add(card);
            Debug.Log($"[DeckManager] 형용사 카드 버림: {card.Name}");
        }
        else if (card.Type == CardType.Gerund)
        {
            GerundDiscardPile.Add(card);
            Debug.Log($"[DeckManager] 동명사 카드 버림: {card.Name}");
        }
        else
        {
            Debug.LogWarning($"[DeckManager] 버릴 수 없는 알 수 없는 카드 타입: {card.Type}");
        }
    }

    // 턴 종료: 손패에 남은 카드를 모두 버린 더미로
    public void DiscardHand()
    {
        // 도는 중에 목록이 바뀌지 않도록 복사해서 순회
        var cardsToDiscard = new List<ICard>(Hand);

        foreach (var card in cardsToDiscard)
        {
            DiscardCard(card);
        }

        Hand.Clear();
        Debug.Log("[DeckManager] 턴 종료: 손패를 모두 비우고 버린 덱으로 보냈습니다.");
    }

    // 버린 더미의 카드를 모두 뽑을 더미로 옮기고 섞는다
    public void RefillDeckFromDiscard(bool isAdjective)
    {
        var drawPile = isAdjective ? AdjectiveDrawPile : GerundDrawPile;
        var discardPile = isAdjective ? AdjectiveDiscardPile : GerundDiscardPile;

        if (discardPile.Count == 0)
        {
            Debug.LogWarning($"[DeckManager] {(isAdjective ? "형용사" : "동명사")} 버린 덱이 비어있어 리필할 수 없습니다.");
            return;
        }

        drawPile.AddRange(discardPile);

        discardPile.Clear();

        Shuffle(drawPile);

        Debug.Log($"[DeckManager] {(isAdjective ? "형용사" : "동명사")} 버린 덱을 섞어서 뽑을 덱으로 리필 완료!");
    }

    // 조합에 쓴 슬롯의 재료 카드 두 장을 버린 더미로
    public void UseCardsForCombination()
    {
        if (AdjectiveSlot != null)
        {
            DiscardCard(AdjectiveSlot);
            AdjectiveSlot = null;
        }

        if (GerundSlot != null)
        {
            DiscardCard(GerundSlot);
            GerundSlot = null;
        }
    }

    // 슬롯에 카드를 올렸을 때 기록
    public void EquipCardToSlot(ICard card, SlotType slotType)
    {
        if (card == null) return;

        if (slotType == SlotType.Adjective)
        {
            AdjectiveSlot = card;
            Debug.Log($"[DeckManager] 형용사 슬롯에 장착됨: {card.Name}");
        }
        else if (slotType == SlotType.Gerund)
        {
            GerundSlot = card;
            Debug.Log($"[DeckManager] 동명사 슬롯에 장착됨: {card.Name}");
        }
    }

    // 슬롯 비우기 (카드를 다시 손패로 뺐을 때)
    public void ClearSlot(SlotType slotType)
    {
        if (slotType == SlotType.Adjective)
        {
            AdjectiveSlot = null;
            Debug.Log("[DeckManager] 형용사 슬롯 비워짐");
        }
        else if (slotType == SlotType.Gerund)
        {
            GerundSlot = null;
            Debug.Log("[DeckManager] 동명사 슬롯 비워짐");
        }
    }

    // 디버그: 모든 더미와 손패 내용을 로그로 출력
    public void DebugShowAllCards()
    {
        Debug.Log("========== [Deck Status Debug] ==========");

        Debug.Log($"--- [Adjective] DrawPile (Count: {AdjectiveDrawPile.Count}) ---");
        for (int i = 0; i < AdjectiveDrawPile.Count; i++)
        {
            Debug.Log($"[{i}] {AdjectiveDrawPile[i].Name}");
        }

        Debug.Log($"--- [Adjective] DiscardPile (Count: {AdjectiveDiscardPile.Count}) ---");
        for (int i = 0; i < AdjectiveDiscardPile.Count; i++)
        {
            Debug.Log($"[{i}] {AdjectiveDiscardPile[i].Name}");
        }

        Debug.Log($"--- [Gerund] DrawPile (Count: {GerundDrawPile.Count}) ---");
        for (int i = 0; i < GerundDrawPile.Count; i++)
        {
            Debug.Log($"[{i}] {GerundDrawPile[i].Name}");
        }

        Debug.Log($"--- [Gerund] DiscardPile (Count: {GerundDiscardPile.Count}) ---");
        for (int i = 0; i < GerundDiscardPile.Count; i++)
        {
            Debug.Log($"[{i}] {GerundDiscardPile[i].Name}");
        }

        Debug.Log($"--- Hand (Count: {Hand.Count}) ---");
        for (int i = 0; i < Hand.Count; i++)
        {
            Debug.Log($"[{i}] {Hand[i].Name}");
        }

        Debug.Log("=========================================");
    }
}