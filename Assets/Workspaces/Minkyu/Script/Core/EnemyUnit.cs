using System;
using UnityEngine;

public class EnemyUnit : UnitBase
{
    public int attackPower = 4;

    // 기믹용 이벤트 (BossController2 등에서 구독)
    // OnDamaged(받은 공격량, 맞기 직전 방어도) : 가시 갑옷 등
    public event Action<int, int> OnDamaged;
    // OnAttackHit(플레이어 체력에 실제로 들어간 피해) : 흡혈 등
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
        // ⭐️ 씬에 생성되자마자 GameManager가 있는지 확인하고 자기 자신을 찔러넣음
        //if (GameManager.Instance != null)
        //{
        //    GameManager.Instance.RegisterEnemy(this);
        //}

        // 스탯이 주입된 유닛만 UI에 연결 (외형 프리팹 등에 잘못 붙은 EnemyUnit이 체력바를 가로채지 않도록)
        if (UIManager.Instance != null && statData != null)
        {
            UIManager.Instance.LinkUnitToUI(this);
        }

        

        Debug.Log($"[생성됨] 적 이름: {gameObject.name}, 인스턴스ID: {GetInstanceID()}");
    }

    public override void TakeDamage(int damage)
    {
        int shieldBefore = statData != null ? statData.currentShield : 0;

        // 테스트용 한 방 데미지 (LobbyManager의 Debug Big Damage)
        if (GameFlowData.debugBigDamage && damage > 0)
            damage = Mathf.Max(damage, GameFlowData.DebugBigDamageAmount);

        base.TakeDamage(damage);
        OnDamaged?.Invoke(damage, shieldBefore);
    }

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
