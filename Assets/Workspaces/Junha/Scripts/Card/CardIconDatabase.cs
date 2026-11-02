using System;
using System.Collections.Generic;
using UnityEngine;

// 카드별 중앙 아이콘 표 (500FreeSkillIcons 에셋). 형용사 / 동명사는 서버 id, 조합은 combinationId로 찾는다
[CreateAssetMenu(fileName = "CardIconDatabase", menuName = "DrawOrDie/Card Icon Database")]
public class CardIconDatabase : ScriptableObject
{
    [Serializable]
    public class IdIcon
    {
        public string name; // 인스펙터 목록에 보이는 이름 (카드 이름)
        public int id;
        public Sprite icon;
    }

    [Serializable]
    public class CombinationIcon
    {
        public string name; // 인스펙터 목록에 보이는 이름 (스킬 이름)
        public string combinationId;
        public Sprite icon;
    }

    [Tooltip("지정되지 않은 카드에 쓸 아이콘. 비워두면 아이콘 없이 막힌 프레임으로 표시")]
    public Sprite defaultIcon;

    public List<IdIcon> adjectives = new List<IdIcon>();
    public List<IdIcon> gerunds = new List<IdIcon>();
    public List<CombinationIcon> combinations = new List<CombinationIcon>();

    // 표에 없으면 defaultIcon
    public Sprite GetAdjectiveIcon(int id) => Fallback(adjectives.Find(e => e.id == id)?.icon);
    public Sprite GetGerundIcon(int id) => Fallback(gerunds.Find(e => e.id == id)?.icon);
    public Sprite GetCombinationIcon(string combinationId) => Fallback(combinations.Find(e => e.combinationId == combinationId)?.icon);

    public Sprite GetIcon(ICard card)
    {
        switch (card.Type)
        {
            case CardType.Adjective: return GetAdjectiveIcon(card.Id);
            case CardType.Gerund: return GetGerundIcon(card.Id);
            default: return defaultIcon;
        }
    }

    // 아이콘이 없으면 기본 아이콘 (기본 아이콘도 없으면 null → 막힌 프레임)
    private Sprite Fallback(Sprite icon) => icon != null ? icon : defaultIcon;
}
