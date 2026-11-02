using System;
using System.Collections.Generic;

// 서버 Relic.json의 유물 하나 (JsonUtility용이라 필드 이름이 서버 JSON과 같아야 함)
[Serializable]
public class RelicData
{
    public int id;
    public string key; // 유물 고유 키 (보유 목록 / 세이브 / 아이콘 연결에 사용)
    public string name;
    public string description;
    public string effectType;
    public string trigger; // 발동 시점 (예: 전투 시작, 주사위, 턴 종료, 승리)
    public RelicCondition condition;
    public RelicEffect effect;
    public bool stackable; // 여러 개 가질 수 있는지
}

// 발동 조건 (예: 주사위 눈이 몇 이상)
[Serializable]
public class RelicCondition
{
    public string type;
    public int value;
}

// 효과 (예: 에너지 +N, 방어도 +N)
[Serializable]
public class RelicEffect
{
    public string type;
    public int value;
}

// 서버 유물 목록 응답
[Serializable]
public class RelicListResponse
{
    public List<RelicData> relics;
}
