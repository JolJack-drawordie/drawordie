using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    // 형용사 카드 더미
    public List<ICard> AdjectiveDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> AdjectiveDiscardPile { get; private set; } = new List<ICard>();

    // 동명사 카드 더미
    public List<ICard> GerundDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> GerundDiscardPile { get; private set; } = new List<ICard>();

    public List<ICard> Hand { get; private set; } = new List<ICard>();

    public void Awake()
    {
        Instance = this;
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

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        // 카드 덱 맨 위의 카드를 뽑아 패(Hand)로 이동
        if (drawPile.Count > 0)
        {
            ICard card = drawPile[0];
            Hand.Add(card);
            drawPile.RemoveAt(0);
        }
    }

    public BattleStartHand PrepareInitialHand()
    {
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
}