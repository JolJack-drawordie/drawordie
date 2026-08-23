using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    public bool IsDeckInitialized { get; set; } = false;

    // 형용사 카드 더미
    public List<ICard> AdjectiveDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> AdjectiveDiscardPile { get; private set; } = new List<ICard>();

    // 동명사 카드 더미
    public List<ICard> GerundDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> GerundDiscardPile { get; private set; } = new List<ICard>();

    public List<ICard> Hand { get; private set; } = new List<ICard>();

    public ICard AdjectiveSlot; // 형용사 슬롯에 올라간 카드
    public ICard GerundSlot;    // 동명사 슬롯에 올라간 카드 (또는 복수일 경우 List<ICard>)

    public void Awake()
    {
        // 싱글톤 중복 방지 로직
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않게 설정!
        }
        else
        {
            Destroy(gameObject); // 이미 존재하면 새로 만들어진 것은 파괴
        }
    }
    
    public void InitializeDeck(List<int> adjectiveIds, List<int> gerundIds)
    {
        AdjectiveDrawPile.Clear(); // 혹시 모를 기존 카드 초기화
        GerundDrawPile.Clear();

        foreach (int id in adjectiveIds)
        {
            var data = DataManager.Instance.adjectiveTable[id];
            AdjectiveDrawPile.Add(new AdjectiveCard(data));
        }

        foreach (int id in gerundIds)
        {
            var data = DataManager.Instance.gerundTable[id];
            GerundDrawPile.Add(new GerundCard(data));
        }
    }
    public void ShuffleDeck()
    {
        Shuffle(AdjectiveDrawPile);
        Shuffle(GerundDrawPile);
    }

    private void Shuffle(List<ICard> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            ICard temp = list[rnd];
            list[rnd] = list[i];
            list[i] = temp;
        }
    }

    public void DrawHand()
    {

        // 1. 형용사 카드 2장 뽑기
        for (int i = 0; i < 2; i++)
        {
            DrawCard(true);
        }

        // 2. 동명사 카드 3장 뽑기
        for (int i = 0; i < 3; i++)
        {
            DrawCard(false);
        }
    }

    public void DrawCard(bool isAdjective)
    {
        var drawPile = isAdjective ? AdjectiveDrawPile : GerundDrawPile;
        var discardPile = isAdjective ? AdjectiveDiscardPile : GerundDiscardPile;

        // 뽑을 더미가 비었으면 버린 더미를 섞어서 다시 채움
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return; // 둘 다 비었으면 뽑을 카드가 없음

            RefillDeckFromDiscard(isAdjective);
        }

        // 카드 덱 맨 위의 카드를 뽑아 패(Hand)로 이동
        if (drawPile.Count > 0)
        {
            ICard card = drawPile[0];
            Hand.Add(card);
            drawPile.RemoveAt(0);

            Debug.Log($"[Draw] {card.Name} (타입: {card.Type}) 뽑음! 남은 카드 수: {drawPile.Count}");
        }
    }

    public BattleStartHand PrepareInitialHand()
    {
        Hand.Clear();
        // 1. 처음에 카드 뽑기 (이미 덱에 카드 객체인 AdjectiveCard, GerundCard가 들어있다고 가정)
        // 형용사 2장, 동명사 3장을 뽑아서 Hand에 저장
        DrawHand();

        // 2. 전투에 필요한 데이터 형태로 패키징
        BattleStartHand handData = new BattleStartHand();
        handData.adjectives = new List<Adjective>();
        handData.gerunds = new List<Gerund>();

        // 3. Hand에 있는 카드 객체들에서 알맹이(Data)만 쏙 빼서 전달
        foreach (var card in Hand)
        {
            if (card is AdjectiveCard adjCard)
            {
                // AdjectiveCard 객체에서 데이터(Adjective)를 뽑아서 추가
                handData.adjectives.Add(adjCard.GetData()); // GetData 메서드를 하나 추가하면 편리함
            }
            else if (card is GerundCard gerCard)
            {
                handData.gerunds.Add(gerCard.GetData());
            }
        }

        return handData;
    }

    public void AddCardToDeck(ICard chosenCard)
    {
        if (chosenCard == null)
        {
            Debug.LogError("[DeckManager] 추가하려는 카드가 null입니다!");
            return;
        }

        // 카드의 Type을 확인하여 알맞은 더미에 추가
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

    public void DiscardCard(ICard card)
    {
        if (card == null)
        {
            Debug.LogError("[DeckManager] 버리려는 카드가 null입니다!");
            return;
        }

        // 1. 손패(Hand)에 있었다면 제거
        if (Hand.Contains(card))
        {
            Hand.Remove(card);
        }

        // 2. 카드 타입에 따라 알맞은 DiscardPile로 이동
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

    // 턴 종료 시 손패에 남은 카드들을 싹 다 버리는 메서드
    public void DiscardHand()
    {
        // 손패 리스트가 foreach 도중에 수정되면 에러 나니까 복사해서 안전하게 순회
        var cardsToDiscard = new List<ICard>(Hand);

        foreach (var card in cardsToDiscard)
        {
            DiscardCard(card); // 위에 만든 공통 메서드 재사용!
        }

        Hand.Clear();
        Debug.Log("[DeckManager] 턴 종료: 손패를 모두 비우고 버린 덱으로 보냈습니다.");
    }

    // 버린 카드 더미(DiscardPile)의 카드들을 뽑을 덱(DrawPile)으로 옮기고 섞는 전용 메서드
    public void RefillDeckFromDiscard(bool isAdjective)
    {
        var drawPile = isAdjective ? AdjectiveDrawPile : GerundDrawPile;
        var discardPile = isAdjective ? AdjectiveDiscardPile : GerundDiscardPile;


        if (discardPile.Count == 0)
        {
            Debug.LogWarning($"[DeckManager] {(isAdjective ? "형용사" : "동명사")} 버린 덱이 비어있어 리필할 수 없습니다.");
            return;
        }

        // 1. 버린 덱의 카드들을 뽑을 덱으로 전부 가져오기
        drawPile.AddRange(discardPile);

        // 2. 버린 덱은 싹 비우기
        discardPile.Clear();

        // 3. 뽑을 덱 섞기
        Shuffle(drawPile);
        
        Debug.Log($"[DeckManager] {(isAdjective ? "형용사" : "동명사")} 버린 덱을 섞어서 뽑을 덱으로 리필 완료!");
    }

    // 카드 조합 시 슬롯에 있는 카드들을 처리하는 메서드
    public void UseCardsForCombination()
    {
        if (AdjectiveSlot != null)
        {
            DiscardCard(AdjectiveSlot);
            AdjectiveSlot = null; // 혹은 ClearSlot(SlotType.Adjective);
        }

        if (GerundSlot != null)
        {
            DiscardCard(GerundSlot);
            GerundSlot = null; // 혹은 ClearSlot(SlotType.Gerund);
        }
    }

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

    // 슬롯 타입에 따라 슬롯을 비우기만 하는 메서드
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

    public void DebugShowAllCards()
    {
        Debug.Log("========== [Deck Status Debug] ==========");

        // 1. 형용사 덱 디버깅
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

        // 2. 동명사 덱 디버깅
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

        // 3. 현재 손패 디버깅
        Debug.Log($"--- Hand (Count: {Hand.Count}) ---");
        for (int i = 0; i < Hand.Count; i++)
        {
            Debug.Log($"[{i}] {Hand[i].Name}");
        }

        Debug.Log("=========================================");
    }
}