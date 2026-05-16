using System.Collections.Generic;

// --- 1. 형용사(Adjective) 데이터 구조 ---
[System.Serializable]
public class AdjectiveInfo 
{
    public int id;
    public string name;
    public int cost;     // ⭐ costMod를 cost로 이름을 바꿨습니다!
    public int dmgMod;   
    public string desc;  
}

[System.Serializable]
public class AdjectiveList 
{ 
    public List<AdjectiveInfo> adjectives; 
}


// --- 2. 동사(Verb) 데이터 구조 ---
[System.Serializable]
public class VerbInfo 
{
    public int id;
    public string name;
    public int baseCost;  // 기본 코스트
    public int baseValue; // 기본 위력 (데미지나 방어량)
    public string action; // 행동 타입 (Attack, Defend 등)
    public string desc;   // 설명
}

[System.Serializable]
public class VerbList 
{ 
    public List<VerbInfo> verbs; 
}