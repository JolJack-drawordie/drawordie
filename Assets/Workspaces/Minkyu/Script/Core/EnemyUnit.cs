using System;
using UnityEngine;

// 적 유닛. 공격력을 들고 있고, 보스 기믹이 쓰는 피격 / 공격 이벤트를 알린다.
public class EnemyUnit : UnitBase
{
    public int attackPower = 4; // 이번 턴 공격력 (적 컨트롤러가 의도에 맞춰 바꿈)

    // 보스 기믹용 이벤트 (BossController2가 구독)
    // OnDamaged(받은 공격량, 맞기 직전 방어도): 가시 갑옷
    public event Action<int, int> OnDamaged;
    // OnAttackHit(플레이어 체력에 실제로 들어간 피해): 흡혈
    public event Action<int> OnAttackHit;

    // 몬스터 이름은 서버 데이터에 영어로만 있어서 노드 종류로 구분해 표시
    public override string LogName
    {
        get
        {
            switch (GameFlowData.currentNodeType)
            {
                case MapNode.NodeType.Boss: return "보스";
                case MapNode.NodeType.Elite: return "중간보스";
                default: return "적";
            }
        }
    }

    public void Start()
    {
        // 스탯이 들어온 유닛만 체력 바에 연결 (외형 프리팹 등에 잘못 붙은 EnemyUnit이 체력 바를 가로채지 않도록)
        if (UIManager.Instance != null && statData != null)
        {
            UIManager.Instance.LinkUnitToUI(this);
        }

        Debug.Log($"[생성됨] 적 이름: {gameObject.name}, 인스턴스ID: {GetInstanceID()}");
    }

    // 피해를 받고 가시 갑옷용 이벤트를 알린다
    public override void TakeDamage(int damage)
    {
        int shieldBefore = statData != null ? statData.currentShield : 0;

        // 테스트용 한 방 피해 (LobbyManager의 Debug Big Damage)
        if (GameFlowData.debugBigDamage && damage > 0)
            damage = Mathf.Max(damage, GameFlowData.DebugBigDamageAmount);

        base.TakeDamage(damage);
        OnDamaged?.Invoke(damage, shieldBefore);
    }

    // 플레이어 공격. 체력에 실제로 들어간 피해만큼 흡혈 이벤트를 알린다
    public void Attack(PlayerUnit target)
    {
        if (target == null) return;

        Debug.Log("적 공격!");

        int hpBefore = target.statData != null ? target.statData.currentHp : 0;
        target.TakeDamage(attackPower);
        int dealt = target.statData != null ? hpBefore - target.statData.currentHp : 0;

        if (dealt > 0) OnAttackHit?.Invoke(dealt);
    }
}
