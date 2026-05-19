using System;
using System.Collections.Generic;

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

// 🔥 서버가 보내주는 최상위 포장 박스
[Serializable]
public class BattleStartHand 
{ 
    public List<Adjective> adjectives; 
    public List<Gerund> gerunds;
}