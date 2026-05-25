using System;
using System.Collections.Generic;
using UnityEngine;

// --- 서버에서 받아올 데이터 구조 ---
[Serializable]
public class Adjective 
{
    public int id;
    public string name;
    public int costMod; // 서버 변수명과 일치!
    public int dmgMod;
    public int shdMod;
    public int healMod;
    public string desc;  
}

[Serializable]
public class Gerund 
{
    public int id;
    public string name;
    public int baseCost; 
    public int baseDmg;
    public int baseShd;
    public int baseHeal;
    public string desc;   
}

[Serializable]
public class Combination //홍성구 추가
{
    public string combinationId;
    public int adjectiveId;
    public int gerundId;
    public string skillName;
    public int finalCost;
    public int finalDamage;
    public int finalShield;
    public int finalHeal;
    public string description;
}

// 🔥 서버가 보내주는 최상위 포장 박스
[Serializable]
public class BattleStartHand 
{ 
    public List<Adjective> adjectives; 
    public List<Gerund> gerunds;
}

[Serializable]
public class CombinationList
{
    public List<Combination> combinations;
}