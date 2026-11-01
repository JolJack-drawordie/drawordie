using System.Collections.Generic;
using UnityEngine;

// 몬스터 머리 위 의도 아이콘을 한 곳에서 관리하는 데이터베이스 (Resources/IntentIconSet_Default)
// - 수치(damage / defend)는 서버 Monster.json에서 오고, 어떤 아이콘을 보여줄지는 여기서 결정
// - 몬스터는 서버 Monster.json과 같은 id(monsterId)로 찾음
//
// 아이콘 결정 순서 (공격/방어)
//   ① 그 몬스터의 수치 구간(attackTiers/defendTiers)
//   ② 공용 수치 구간(commonAttackTiers/commonDefendTiers)
//   ③ 그 몬스터의 기본 아이콘(attackSprite/defendSprite)
//   ④ 공용 기본 아이콘(defaultAttackSprite/defaultDefendSprite)
//   ⑤ 프리팹 Inspector의 Attack/Defend Sprite (예전 방식, 비상용)
// 강공격/기 모으기: 몬스터 항목의 strongAttackSprite/chargeSprite → 프리팹 Inspector 값
//
// 구간은 "값 이상이면 이 아이콘" 방식. 가장 낮은 minValue보다 작은 값은 다음 순서로 넘어감
[CreateAssetMenu(menuName = "DrawOrDie/Intent Icon Set", fileName = "IntentIconSet_Default")]
public class IntentIconSet : ScriptableObject
{
    [System.Serializable]
    public struct Tier
    {
        [Tooltip("이 값 이상이면 아래 스프라이트 사용")]
        public int minValue;
        public Sprite sprite;
    }

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

    // Resources 폴더의 IntentIconSet_Default (없으면 null)
    public static IntentIconSet Default
    {
        get
        {
            if (cachedDefault == null)
                cachedDefault = Resources.Load<IntentIconSet>(DefaultResourcePath);
            return cachedDefault;
        }
    }

    public MonsterIcons Find(int monsterId)
    {
        if (monsters == null) return null;
        foreach (var m in monsters)
            if (m != null && m.monsterId == monsterId) return m;
        return null;
    }

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

    // 공격/방어 아이콘 결정 (prefabFallback = 프리팹 Inspector 값)
    public static Sprite Resolve(int monsterId, bool isAttack, int value, Sprite prefabFallback)
    {
        IntentIconSet db = Default;
        if (db == null) return prefabFallback;

        MonsterIcons m = db.Find(monsterId);

        // ④·⑤ 기본값
        Sprite sprite = isAttack ? db.defaultAttackSprite : db.defaultDefendSprite;
        if (sprite == null) sprite = prefabFallback;

        // ③ 몬스터 기본 아이콘
        if (m != null)
        {
            Sprite own = isAttack ? m.attackSprite : m.defendSprite;
            if (own != null) sprite = own;
        }

        // ② 공용 구간
        sprite = Pick(isAttack ? db.attackTiers : db.defendTiers, value, sprite);

        // ① 몬스터 구간
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
