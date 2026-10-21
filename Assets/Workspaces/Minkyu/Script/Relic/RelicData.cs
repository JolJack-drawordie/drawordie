using System;
using System.Collections.Generic;

[Serializable]
public class RelicData
{
    public int id;
    public string key;
    public string name;
    public string description;
    public string effectType;
    public string trigger;
    public RelicCondition condition;
    public RelicEffect effect;
    public bool stackable;
}

[Serializable]
public class RelicCondition
{
    public string type;
    public int value;
}

[Serializable]
public class RelicEffect
{
    public string type;
    public int value;
}

[Serializable]
public class RelicListResponse
{
    public List<RelicData> relics;
}
