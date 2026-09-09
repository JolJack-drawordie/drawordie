using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MonsterServerData
{
    public int id;
    public string name;
    public int hp;
    public int shield;
}

[System.Serializable]
public class MonsterServerDataListWrapper
{
    public List<MonsterServerData> monsters;
}