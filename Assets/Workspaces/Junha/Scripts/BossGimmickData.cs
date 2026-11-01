using System.Collections.Generic;

// 서버 Monster.json의 "boss" 블록 (중간보스/보스 기믹 수치)
// - 서버는 블록/기믹이 있을 때만 "defined": true 를 자동으로 붙여 보냄 (JSON 파일에 직접 쓸 필요 없음)
// - boss 블록이 있으면 서버가 기준: 블록에 없는 기믹은 꺼짐
// - boss 블록이 없으면(defined == false) 프리팹 Inspector 값을 그대로 사용
// - 숫자가 -1이면(= JSON에 안 적음) 그 항목만 프리팹 Inspector 값을 사용
[System.Serializable]
public class BossGimmickData
{
    public bool defined;

    // 행동 순서. 예: ["Attack","Attack","Defend"]. 비우면 공격/방어(+기 모으기) 랜덤
    public List<string> pattern = new List<string>();

    public ChargeGimmick charge = new ChargeGimmick();
    public RegenGimmick regen = new RegenGimmick();
    public EnrageGimmick enrage = new EnrageGimmick();
    public LifestealGimmick lifesteal = new LifestealGimmick();
    public ThornsGimmick thorns = new ThornsGimmick();
    public Phase2Gimmick phase2 = new Phase2Gimmick();

    [System.Serializable]
    public class ChargeGimmick
    {
        public bool defined;
        public float chance = -1f;       // 패턴을 안 쓸 때 기 모으기 확률 (0~1)
        public float multiplier = -1f;   // 강공격 배율
    }

    [System.Serializable]
    public class RegenGimmick
    {
        public bool defined;
        public int amount = -1;          // 매 턴 회복량
    }

    [System.Serializable]
    public class EnrageGimmick
    {
        public bool defined;
        public float hpRatio = -1f;           // 이 체력 비율 이하에서 분노
        public float attackMultiplier = -1f;  // 분노 시 공격 배율
    }

    [System.Serializable]
    public class LifestealGimmick
    {
        public bool defined;
        public float ratio = -1f;        // 준 피해 중 회복 비율 (0~1)
    }

    [System.Serializable]
    public class ThornsGimmick
    {
        public bool defined;
        public int damage = -1;          // 방어도가 있을 때 맞으면 반사 피해
    }

    [System.Serializable]
    public class Phase2Gimmick
    {
        public bool defined;
        public float hpRatio = -1f;      // 이 체력 비율 이하에서 1회 전환
        public int shield = -1;          // 전환 시 얻는 방어도
        public int heal = -1;            // 전환 시 회복량
        public List<string> pattern = new List<string>(); // 2페이즈 행동 순서 (비우면 기존 유지)
    }
}
