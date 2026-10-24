using System;
using System.Collections.Generic;
using UnityEngine;

// 공격(스킬) 하나의 상세 정보
[Serializable]
public class AttackData
{
    public string skillId;
    public string name;
    public int damage;
    public int cooldown;
}

[System.Serializable]
public class MonsterServerData
{
    public int id;
    public string name;
    public int hp;
    public int shield;

    public List<AttackData> attacks;

    public int defend;
}

[System.Serializable]
public class MonsterServerDataListWrapper
{
    public List<MonsterServerData> monsters;
}