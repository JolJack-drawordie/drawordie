using System;
using System.Collections.Generic;

// 카드 종류: 형용사(보정 카드) / 동명사(행동 카드) / 조합(형용사 + 동명사)
public enum CardType { Adjective, Gerund, Synergy }

// 카드 프레임 색 구분: 형용사 = Basic, 동명사 = Attack / Defense / Heal, 조합 = Combination
public enum CardCategory { Basic, Attack, Defense, Heal, Combination }

// ===== 서버에서 받는 데이터 (JsonUtility용이라 필드 이름이 서버 JSON과 같아야 함) =====
// 형용사 카드: 동명사 카드에 더해지는 비용 / 피해 / 방어 / 회복 보정값
[Serializable]
public class Adjective 
{
    public int id;
    public string name;
    public int costMod;
    public int dmgMod;
    public int shdMod;
    public int healMod;
    public string desc;  
}

// 동명사 카드: 실제 행동(공격 / 방어 / 회복)의 기본 수치
[Serializable]
public class Gerund 
{
    public int id;
    public string name;
    public string category; // "ATTACK" / "DEFENSE" / "HEAL"
    public int baseCost; 
    public int baseDmg;
    public int baseShd;
    public int baseHeal;
    public string desc;   
}

// 형용사 + 동명사 조합 결과 (서버가 최종 수치까지 계산해서 보냄)
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

// 전투 시작 손패 (JsonUtility는 최상위 배열을 못 읽어서 객체로 감싸서 받음)
[Serializable]
public class BattleStartHand 
{ 
    public List<Adjective> adjectives; 
    public List<Gerund> gerunds;
}

// 아래는 목록 API 응답용 포장 클래스
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

// 손패 / 덱에서 쓰는 카드 공통 인터페이스 (형용사 카드, 동명사 카드)
public interface ICard
{
    int Id { get; }
    CardType Type { get; } // 형용사 / 동명사 / 조합
    CardCategory Category { get; } // 카드 프레임 색 구분

    // 카드에 표시하는 값
    string Name { get; }
    int Cost { get; }
    string Description { get; }

    // 사용했을 때의 효과 수치
    int Damage { get; }
    int Shield { get; }
    int Heal { get; }
}

// 형용사 카드 (서버 Adjective 데이터를 감쌈)
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

    public int Id => _data.id;
    public CardType Type => CardType.Adjective;
    public CardCategory Category => CardCategory.Basic;
    public string Name => _data.name;
    public int Cost => _data.costMod;
    public string Description => _data.desc;
    public int Damage => _data.dmgMod;
    public int Shield => _data.shdMod;
    public int Heal => _data.healMod;
}

// 동명사 카드 (서버 Gerund 데이터를 감쌈)
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
}

// 카드 설명 아래에 "(공격 N, 방어 N, 치유 N)" 수치 줄을 붙인다 (0인 값은 생략).
// 형용사는 단독으로 쓰지 않는 보정 카드라 설명만 보여준다.
public static class CardText
{
    public static string Describe(ICard card)
    {
        return Describe(card.Type, card.Description, card.Damage, card.Shield, card.Heal);
    }

    public static string Describe(Combination combo)
    {
        return WithStats(combo.description, combo.finalDamage, combo.finalShield, combo.finalHeal);
    }

    public static string Describe(CardType type, string desc, int damage, int shield, int heal)
    {
        return type == CardType.Adjective ? desc : WithStats(desc, damage, shield, heal);
    }

    private static string WithStats(string desc, int damage, int shield, int heal)
    {
        List<string> stats = new List<string>();
        if (damage > 0) stats.Add($"공격 {damage}");
        if (shield > 0) stats.Add($"방어 {shield}");
        if (heal > 0) stats.Add($"치유 {heal}");
        else if (heal < 0) stats.Add($"체력 {heal}"); // 체력 소모 카드 (독한 조합 카드 등)

        if (stats.Count == 0) return desc;

        string statLine = $"({string.Join(", ", stats)})";
        return string.IsNullOrEmpty(desc) ? statLine : $"{desc}\n{statLine}";
    }
}

// 새 게임 시작 덱 (서버 응답)
[Serializable]
public class DefaultDeckData
{
    public List<int> adjectiveIds;
    public List<int> gerundIds;
}