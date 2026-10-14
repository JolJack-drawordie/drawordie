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

    // 손패에 있는 조합 카드 (덱에 속하지 않고 손패 UI로만 존재하므로 따로 저장)
    public List<ComboSaveData> comboHand = new List<ComboSaveData>();
}

// 조합 카드는 재료 형용사 / 동명사 ID로 저장하고, 불러올 때 조합표에서 다시 찾아 만든다
[Serializable]
public class ComboSaveData
{
    public int adjectiveId;
    public int gerundId;

    public ComboSaveData(int adjectiveId, int gerundId)
    {
        this.adjectiveId = adjectiveId;
        this.gerundId = gerundId;
    }
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

// ⭐ [로드 기능] 신규 추가
// 서버 GameSave 엔티티 응답(JSON)을 그대로 받기 위한 DTO
[Serializable]
public class GameSaveDto
{
    public long userId;
    public int masterSeed;
    public int mapSeed;
    public int nodeSeed;
    public int currentHp;
    public int currentShield;
    public int currentCost;
    public int maxCost; // 이번 턴 최대 에너지 (구버전 세이브는 0)
    public string deckData;
    public string monsterData;
    public int currentFloor;
    public int currentIndex;
    public int currentAct;
    public int currentNodeType;
    public bool rested; // 현재 휴식 노드에서 이미 휴식했는지
    public int playTime; // 누적 플레이 타임(초)
}

// 서버 /api/game/result 응답: 방금 등록한 기록의 순위
[Serializable]
public class GameResultResponse
{
    public long resultId;
    public long rank;
    public int score;
}

// 서버 /api/game/ranking 응답의 기록 한 줄
[Serializable]
public class RankingEntry
{
    public long resultId;
    public int rank;
    public string nickname;
    public int score;
    public bool cleared;
    public int reachedAct;
    public int reachedFloor;
    public int playTime; // 초
    public string endedAt; // "2026-09-27T12:34:56"
}

[Serializable]
public class RankingListDto
{
    public List<RankingEntry> rankings;
}
