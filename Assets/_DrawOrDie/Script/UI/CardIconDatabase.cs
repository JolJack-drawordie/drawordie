using System;
using System.Collections.Generic;
using UnityEngine;

// 카드별 중앙 아이콘 매핑 테이블 (500FreeSkillIcons)
[CreateAssetMenu(fileName = "CardIconDatabase", menuName = "DrawOrDie/Card Icon Database")]
public class CardIconDatabase : ScriptableObject
{
    [Serializable]
    public class IdIcon
    {
        public string name; // 인스펙터 표시용 (카드 이름)
        public int id;
        public Sprite icon;
    }

    [Serializable]
    public class CombinationIcon
    {
        public string name; // 인스펙터 표시용 (스킬 이름)
        public string combinationId;
        public Sprite icon;
    }

    [Tooltip("지정되지 않은 카드에 쓸 아이콘. 비워두면 아이콘 없이 막힌 프레임으로 표시")]
    public Sprite defaultIcon;

    public List<IdIcon> adjectives = new List<IdIcon>();
    public List<IdIcon> gerunds = new List<IdIcon>();
    public List<CombinationIcon> combinations = new List<CombinationIcon>();

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

    private Sprite Fallback(Sprite icon) => icon != null ? icon : defaultIcon;
}
