// 세이브 / 로드 / 랭킹에서 서버와 주고받는 데이터 형식 (JsonUtility용이라 필드 이름이 서버 JSON과 같아야 함)
using System;
using System.Collections.Generic;

// 카드 한 장 (서버 카드 id + 종류)
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

// 덱 전체 상태: 형용사 / 동명사 각각의 뽑을 더미와 버린 더미, 손패, 조합 슬롯에 들어 있는 카드
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

    // 손패에 있는 조합 카드 (덱에 속하지 않고 손패 UI에만 있으므로 따로 저장)
    public List<ComboSaveData> comboHand = new List<ComboSaveData>();
}

// 조합 카드는 재료 형용사 / 동명사 id로 저장하고, 불러올 때 조합표에서 다시 찾아 만든다
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

// 보유 유물은 key 목록으로 저장하고, 불러올 때 서버 유물 데이터에서 다시 찾는다
[Serializable]
public class RelicSaveData
{
    public List<string> relicKeys = new List<string>();
}

// 전투 중 저장했을 때의 적 상태
[Serializable]
public class MonsterSaveData
{
    public string unitName;
    public int currentHp;
    public int maxHp;
    public int currentShield;
    public int maxShield;

    // 다음 행동 (불러온 뒤에도 같은 행동을 하도록). 예전 세이브는 hasIntent = false라서 무작위로 다시 뽑는다
    public bool hasIntent;
    public string intent; // Attack / Defend / Charge / StrongAttack
    public int intentValue; // 일반 몬스터: 공격 피해 또는 방어도

    // 중간보스 / 보스 기믹 진행 상태
    public int patternIndex;
    public bool isEnraged;
    public bool isPhase2;
}

// 서버 세이브(GameSave) 응답을 그대로 받는 형식
[Serializable]
public class GameSaveDto
{
    public long userId;
    public int masterSeed;
    public int mapSeed;
    public int nodeSeed;
    public int currentHp;
    public int maxHp; // 유물로 늘어난 값 포함 (예전 세이브는 0)
    public int currentShield;
    public int currentCost;
    public int maxCost; // 이번 턴 최대 에너지 (예전 세이브는 0)
    public string deckData; // DeckSaveData JSON
    public string monsterData; // MonsterSaveData JSON (전투 중 저장일 때만)
    public string relicData; // RelicSaveData JSON (예전 세이브는 null)
    public int currentFloor;
    public int currentIndex;
    public int currentAct;
    public int currentNodeType; // MapNode.NodeType 값
    public bool rested; // 지금 휴식 노드에서 이미 휴식했는지
    public int playTime; // 누적 플레이 타임(초)
    public int eliteKills; // 처치한 중간보스 수 (예전 세이브는 0)
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
    public int eliteKills; // 처치한 중간보스 수
    public int playTime; // 초
    public string endedAt; // "2026-09-27T12:34:56"
}

// 서버 /api/game/ranking 응답 (기록 목록)
[Serializable]
public class RankingListDto
{
    public List<RankingEntry> rankings;
}
