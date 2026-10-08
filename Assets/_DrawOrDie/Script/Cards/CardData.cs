using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 카드 타입을 구분하기 위한 열거형 추가
public enum CardType { Adjective, Gerund, Synergy }

// 카드 UI 색상/프레임 구분용 (형용사=Basic, 동명사=Attack/Defense/Heal, 조합=Combination)
public enum CardCategory { Basic, Attack, Defense, Heal, Combination }

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
    public string category; // 서버 값: "ATTACK" / "DEFENSE" / "HEAL"
    public int baseCost; 
    public int baseDmg;
    public int baseShd;
    public int baseHeal;
    public string desc;   
}

[Serializable]
public class Combination
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

// 🔥 에러의 원인이었던 최상위 포장 박스 (변수명 소문자 확인!)
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

[Serializable]
public class AdjectiveList
{
    public List<Adjective> adjectives;
}

[Serializable]
public class GerundList
{
    public List<Gerund> gerunds;
}

public interface ICard
{
    int Id { get; }
    CardType Type { get; } // "Adjective", "Gerund", "Combination" 등 구분용
    CardCategory Category { get; } // 카드 프레임 색상 구분용

    // UI 표시용
    string Name { get; }
    int Cost { get; }
    string Description { get; }

    // 효과 계산용
    int Damage { get; }
    int Shield { get; }
    int Heal { get; }

    void Play();
}

public class AdjectiveCard : ICard
{
    private Adjective _data;

    public AdjectiveCard(Adjective data)
    {
        _data = data;
    }

    public Adjective GetData()
    {
        return _data;
    }

    public int Id => _data.id; // 서버 ID가 int라면 .ToString() 사용
    public CardType Type => CardType.Adjective;
    public CardCategory Category => CardCategory.Basic;
    public string Name => _data.name;
    public int Cost => _data.costMod;
    public string Description => _data.desc;
    public int Damage => _data.dmgMod;
    public int Shield => _data.shdMod;
    public int Heal => _data.healMod;

    public void Play()
    {
        // 형용사 카드만의 로직
        Debug.Log($"{Name} 카드 사용! 효과 발동.");
    }
}

public class GerundCard : ICard
{
    private Gerund _data;

    public GerundCard(Gerund data)
    {
        _data = data;
    }

    public Gerund GetData()
    {
        return _data;
    }

    public int Id => _data.id;
    public CardType Type => CardType.Gerund;
    public CardCategory Category => ParseCategory(_data.category);
    public string Name => _data.name;
    public int Cost => _data.baseCost;
    public string Description => _data.desc;
    public int Damage => _data.baseDmg;
    public int Shield => _data.baseShd;
    public int Heal => _data.baseHeal;

    // 서버 문자열을 CardCategory로 변환 (값이 없거나 모르는 값이면 Basic)
    private static CardCategory ParseCategory(string category)
    {
        switch (category?.Trim().ToUpperInvariant())
        {
            case "ATTACK": return CardCategory.Attack;
            case "DEFENSE": return CardCategory.Defense;
            case "HEAL": return CardCategory.Heal;
            default: return CardCategory.Basic;
        }
    }

    public void Play()
    {
        // 동명사 카드만의 로직
        Debug.Log($"{Name} 카드 사용! 효과 발동.");
    }
}

[Serializable]
public class DefaultDeckData
{
    public List<int> adjectiveIds;
    public List<int> gerundIds;
}