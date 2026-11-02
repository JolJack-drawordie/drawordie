using System.Collections.Generic;

// 서버 Monster.json의 "boss" 블록 (중간보스 / 보스 기믹 수치).
// - 서버는 블록이나 기믹이 있을 때만 "defined": true 를 붙여 보낸다 (JSON에 직접 쓸 필요 없음)
// - boss 블록이 있으면 서버 값이 기준이고, 블록에 없는 기믹은 꺼진다
// - boss 블록이 없으면(defined == false) 보스 프리팹의 인스펙터 값을 그대로 쓴다
// - 숫자가 -1이면(JSON에 안 적음) 그 항목만 인스펙터 값을 쓴다
[System.Serializable]
public class BossGimmickData
{
    public bool defined;

    // 행동 순서 (예: ["Attack","Attack","Defend"]). 비우면 공격/방어(+기 모으기)를 무작위로
    public List<string> pattern = new List<string>();

    public ChargeGimmick charge = new ChargeGimmick();
    public RegenGimmick regen = new RegenGimmick();
    public EnrageGimmick enrage = new EnrageGimmick();
    public LifestealGimmick lifesteal = new LifestealGimmick();
    public ThornsGimmick thorns = new ThornsGimmick();
    public Phase2Gimmick phase2 = new Phase2Gimmick();

    // 기 모으기 → 다음 턴 강공격
    [System.Serializable]
    public class ChargeGimmick
    {
        public bool defined;
        public float chance = -1f; // 패턴을 쓰지 않을 때 기 모으기 확률 (0~1)
        public float multiplier = -1f; // 강공격 배율
    }

    // 매 턴 체력 회복
    [System.Serializable]
    public class RegenGimmick
    {
        public bool defined;
        public int amount = -1; // 매 턴 회복량
    }

    // 체력이 일정 비율 이하로 떨어지면 분노 (공격력 증가)
    [System.Serializable]
    public class EnrageGimmick
    {
        public bool defined;
        public float hpRatio = -1f; // 이 체력 비율 이하에서 분노
        public float attackMultiplier = -1f; // 분노 시 공격 배율
    }

    // 흡혈: 준 피해의 일부만큼 회복
    [System.Serializable]
    public class LifestealGimmick
    {
        public bool defined;
        public float ratio = -1f; // 준 피해 중 회복 비율 (0~1)
    }

    // 가시: 방어도가 남아 있을 때 맞으면 공격한 쪽에 반사 피해
    [System.Serializable]
    public class ThornsGimmick
    {
        public bool defined;
        public int damage = -1; // 반사 피해량
    }

    // 2페이즈: 체력이 일정 비율 이하가 되면 한 번 전환 (방어도/회복 + 새 행동 순서)
    [System.Serializable]
    public class Phase2Gimmick
    {
        public bool defined;
        public float hpRatio = -1f; // 이 체력 비율 이하에서 한 번 전환
        public int shield = -1; // 전환 시 얻는 방어도
        public int heal = -1; // 전환 시 회복량
        public List<string> pattern = new List<string>(); // 2페이즈 행동 순서 (비우면 기존 순서 유지)
    }
}
