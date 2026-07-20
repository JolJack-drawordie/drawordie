using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    // 형용사 관련
    public List<ICard> AdjectiveDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> AdjectiveDiscardPile { get; private set; } = new List<ICard>();

    // 동명사 관련
    public List<ICard> GerundDrawPile { get; private set; } = new List<ICard>();
    public List<ICard> GerundDiscardPile { get; private set; } = new List<ICard>();

    public List<ICard> Hand { get; private set; } = new List<ICard>();

    public void Awake()
    {
        Instance = this;
    }
    public void InitializeDeck()
    {
        // 이미 있는 딕셔너리(예: DataManager.gerundTable)에서 데이터를 가져옴
        // 101번 카드 3장
        for (int i = 0; i < 2; i++)
        {
            // 딕셔너리에서 참조(Reference)만 가져와서 카드를 만듦
            var data = DataManager.Instance.adjectiveTable[101];
            AdjectiveDrawPile.Add(new AdjectiveCard(data));
        }

        // 201번 카드 2장
        for (int i = 0; i < 3; i++)
        {
            var data = DataManager.Instance.gerundTable[201];
            GerundDrawPile.Add(new GerundCard(data));
        }

        // 3. 섞기
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
        // 1. 형용사 2장 뽑기
        for (int i = 0; i < 2; i++)
        {
            DrawCard(true);
        }

        // 2. 동명사 3장 뽑기
        for (int i = 0; i < 3; i++)
        {
            DrawCard(false);
        }
    }

    public void DrawCard(bool isAdjective)
    {
        var drawPile = isAdjective ? AdjectiveDrawPile : GerundDrawPile;
        var discardPile = isAdjective ? AdjectiveDiscardPile : GerundDiscardPile;

        // 덱이 비었으면 무덤을 섞어서 다시 채움
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return; // 둘 다 비었으면 뽑을 카드 없음

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        // 카드 한 장을 덱에서 꺼내 손(Hand)으로 이동
        if (drawPile.Count > 0)
        {
            ICard card = drawPile[0];
            Hand.Add(card);
            drawPile.RemoveAt(0);
        }
    }

    public BattleStartHand PrepareInitialHand()
    {
        // 1. 덱에서 카드 뽑기 (이미 덱에 로직 객체인 AdjectiveCard, GerundCard가 들어있다고 가정)
        // 형용사 2장, 동명사 3장 뽑아서 Hand에 넣음
        DrawHand();

        // 2. 팀원이 원하는 형식으로 데이터 패키징
        BattleStartHand handData = new BattleStartHand();
        handData.adjectives = new List<Adjective>();
        handData.gerunds = new List<Gerund>();

        // 3. Hand에 있는 로직 객체들에서 알맹이(Data)만 쏙 빼서 전달
        foreach (var card in Hand)
        {
            if (card is AdjectiveCard adjCard)
            {
                // AdjectiveCard 내부의 데이터(Adjective)를 꺼내서 전달
                handData.adjectives.Add(adjCard.GetData()); // GetData 메서드 하나 추가하면 편해
            }
            else if (card is GerundCard gerCard)
            {
                handData.gerunds.Add(gerCard.GetData());
            }
        }

        return handData;
    }
}

