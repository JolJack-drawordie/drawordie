using System;
using System.Collections.Generic;

[Serializable]
public class CardSaveData
{
    public int id;
    public CardType type;

    public CardSaveData(int id, CardType type)
    {
        this.id = id;
        this.type = type;
    }
}

[Serializable]
public class DeckSaveData
{
    public List<CardSaveData> adjectiveDrawPile = new List<CardSaveData>();
    public List<CardSaveData> adjectiveDiscardPile = new List<CardSaveData>();
    public List<CardSaveData> gerundDrawPile = new List<CardSaveData>();
    public List<CardSaveData> gerundDiscardPile = new List<CardSaveData>();
    public List<CardSaveData> hand = new List<CardSaveData>();

    public bool hasAdjectiveSlotCard;
    public CardSaveData adjectiveSlotCard;

    public bool hasGerundSlotCard;
    public CardSaveData gerundSlotCard;
}

[Serializable]
public class MonsterSaveData
{
    public string unitName;
    public int currentHp;
    public int maxHp;
    public int currentShield;
    public int maxShield;
}
