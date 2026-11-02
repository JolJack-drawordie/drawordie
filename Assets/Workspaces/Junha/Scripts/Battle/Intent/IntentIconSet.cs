using System.Collections.Generic;
using UnityEngine;

// 몬스터 머리 위 의도 아이콘을 한 곳에서 관리하는 에셋 (Resources/IntentIconSet_Default).
// 수치(피해 / 방어도)는 서버 Monster.json에서 오고, 어떤 아이콘을 보여줄지는 여기서 정한다. 몬스터는 서버와 같은 id로 찾는다.
//
// 공격 / 방어 아이콘 결정 순서 (앞이 우선)
//   1) 그 몬스터의 수치 구간 (attackTiers / defendTiers)
//   2) 공용 수치 구간
//   3) 그 몬스터의 기본 아이콘
//   4) 공용 기본 아이콘
//   5) 몬스터 프리팹 인스펙터의 Attack / Defend Sprite (예전 방식, 비상용)
// 강공격 / 기 모으기 아이콘은 몬스터 항목의 값 → 프리팹 인스펙터 값 순서.
// 수치 구간은 "값이 minValue 이상이면 이 아이콘" 방식이고, 가장 낮은 구간보다 작으면 다음 순서로 넘어간다.
[CreateAssetMenu(menuName = "DrawOrDie/Intent Icon Set", fileName = "IntentIconSet_Default")]
public class IntentIconSet : ScriptableObject
{
    // 수치 구간 하나
    [System.Serializable]
    public struct Tier
    {
        [Tooltip("이 값 이상이면 아래 스프라이트 사용")]
        public int minValue;
        public Sprite sprite;
    }

    // 몬스터 한 종류의 아이콘 설정
    [System.Serializable]
    public class MonsterIcons
    {
        [Tooltip("표시용 이름 (알아보기 쉽게만, 동작에는 영향 없음)")]
        public string name;
        [Tooltip("서버 Monster.json의 id와 같아야 함")]
        public int monsterId;

        [Header("기본 아이콘")]
        public Sprite attackSprite;
        public Sprite defendSprite;

        [Header("보스 전용 (비워두면 프리팹 값 사용)")]
        public Sprite strongAttackSprite;
        public Sprite chargeSprite;

        [Header("이 몬스터만의 수치 구간 (선택)")]
        public Tier[] attackTiers;
        public Tier[] defendTiers;
    }

    [Header("공용 기본 아이콘 (몬스터 항목에 아이콘이 없을 때)")]
    public Sprite defaultAttackSprite;
    public Sprite defaultDefendSprite;

    [Header("공용 수치 구간 (모든 몬스터에 적용, 몬스터 기본 아이콘보다 우선)")]
    [Tooltip("공격 데미지 구간 (순서 상관없음, 값 이하인 minValue 중 가장 큰 구간이 선택됨)")]
    public Tier[] attackTiers;
    [Tooltip("방어도 구간 (순서 상관없음, 값 이하인 minValue 중 가장 큰 구간이 선택됨)")]
    public Tier[] defendTiers;

    [Header("몬스터별 아이콘 (id = 서버 Monster.json id)")]
    public List<MonsterIcons> monsters = new List<MonsterIcons>();

    public const string DefaultResourcePath = "IntentIconSet_Default";

    private static IntentIconSet cachedDefault;

    // Resources 폴더의 기본 아이콘 세트 (없으면 null). 한 번 불러오면 캐시
    public static IntentIconSet Default
    {
        get
        {
            if (cachedDefault == null)
                cachedDefault = Resources.Load<IntentIconSet>(DefaultResourcePath);
            return cachedDefault;
        }
    }

    // 서버 몬스터 id로 항목 찾기 (없으면 null)
    public MonsterIcons Find(int monsterId)
    {
        if (monsters == null) return null;
        foreach (var m in monsters)
            if (m != null && m.monsterId == monsterId) return m;
        return null;
    }

    // value 이하인 구간 중 minValue가 가장 큰 구간의 아이콘. 맞는 구간이 없으면 fallback
    private static Sprite Pick(Tier[] tiers, int value, Sprite fallback)
    {
        if (tiers == null) return fallback;

        Sprite result = fallback;
        int bestMin = int.MinValue;
        bool found = false;

        foreach (var t in tiers)
        {
            if (t.sprite == null || value < t.minValue) continue;
            if (!found || t.minValue > bestMin)
            {
                bestMin = t.minValue;
                result = t.sprite;
                found = true;
            }
        }

        return result;
    }

    // 공격/방어 아이콘 결정 (prefabFallback = 몬스터 프리팹 인스펙터 값). 우선순위가 낮은 것부터 차례로 덮어쓴다
    public static Sprite Resolve(int monsterId, bool isAttack, int value, Sprite prefabFallback)
    {
        IntentIconSet db = Default;
        if (db == null) return prefabFallback;

        MonsterIcons m = db.Find(monsterId);

        // 5) 프리팹 값 → 4) 공용 기본 아이콘
        Sprite sprite = isAttack ? db.defaultAttackSprite : db.defaultDefendSprite;
        if (sprite == null) sprite = prefabFallback;

        // 3) 몬스터 기본 아이콘
        if (m != null)
        {
            Sprite own = isAttack ? m.attackSprite : m.defendSprite;
            if (own != null) sprite = own;
        }

        // 2) 공용 수치 구간
        sprite = Pick(isAttack ? db.attackTiers : db.defendTiers, value, sprite);

        // 1) 몬스터 수치 구간
        if (m != null)
            sprite = Pick(isAttack ? m.attackTiers : m.defendTiers, value, sprite);

        return sprite;
    }

    // 보스 강공격 아이콘 (없으면 prefabFallback)
    public static Sprite ResolveStrongAttack(int monsterId, Sprite prefabFallback)
    {
        MonsterIcons m = Default != null ? Default.Find(monsterId) : null;
        return m != null && m.strongAttackSprite != null ? m.strongAttackSprite : prefabFallback;
    }

    // 보스 기 모으기 아이콘 (없으면 prefabFallback)
    public static Sprite ResolveCharge(int monsterId, Sprite prefabFallback)
    {
        MonsterIcons m = Default != null ? Default.Find(monsterId) : null;
        return m != null && m.chargeSprite != null ? m.chargeSprite : prefabFallback;
    }
}
