using System;
using UnityEngine;

// 플레이어 / 적 공통 유닛: 체력, 방어도, 회복, 체력 소모와 전투 로그 / 숫자 팝업 / 체력 바 갱신 이벤트.
// 스탯(UnitStatData)은 StatManager가 만든 런타임 복사본을 Initialize로 넣어 준다.
public class UnitBase : MonoBehaviour
{
    public UnitStatData statData { get; private set; }

    // 체력 / 방어도가 바뀔 때 (체력 바와 적 컨트롤러가 구독)
    public event Action<int, int> OnHpChanged;
    public event Action<int, int> OnShieldChanged;

    // 전투 숫자 팝업용 (DamagePopupManager와 HitShake가 구독)
    public enum NumberKind { Damage, Blocked, Heal, Shield, HpLoss }
    public static event Action<UnitBase, NumberKind, int> OnCombatNumber;

    // 스탯 데이터 연결 (StatManager가 만든 런타임 복사본)
    public virtual void Initialize(UnitStatData data)
    {
        if (data == null)
        {
            Debug.LogError($"{gameObject.name}: 주입된 스탯 데이터가 null입니다!");
            return;
        }

        statData = data;

        UpdateBarUI();
    }

    // 전투 로그에 표시할 이름 (플레이어 / 적 / 보스 등)
    public virtual string LogName => statData != null ? statData.unitName : name;

    // 방어도가 먼저 피해를 막고, 남은 피해만 체력에서 깎는다
    public virtual void TakeDamage(int damage)
    {
        int incoming = damage;
        int shieldBefore = statData.currentShield;

        if (statData.currentShield > 0)
        {
            int tempDamage = damage;
            damage = Mathf.Max(0, damage - statData.currentShield);
            statData.currentShield = Mathf.Max(0, statData.currentShield - tempDamage);
        }
        statData.currentHp -= damage;

        if (statData.currentHp < 0) statData.currentHp = 0;

        // 방어 의도 등으로 공격력이 0인 경우는 기록하지 않음
        if (incoming > 0)
        {
            int blocked = Mathf.Min(incoming, shieldBefore);
            string blockedText = blocked > 0 ? $" (방어도로 {blocked} 막음)" : "";
            BattleLog.Add($"{LogName}에게 {damage} 피해{blockedText}", BattleLog.Kind.Damage);

            // 전부 막혔으면 피해 숫자 없이 "막음"만 표시
            if (damage > 0) OnCombatNumber?.Invoke(this, NumberKind.Damage, damage);
            if (blocked > 0) OnCombatNumber?.Invoke(this, NumberKind.Blocked, blocked);

            // 방어도로 막았을 때 효과음 (플레이어 / 적 공통)
            if (blocked > 0 && SoundManager.Instance != null && SoundManager.Instance.blockSound != null)
                SoundManager.Instance.PlaySFX(SoundManager.Instance.blockSound);
        }

        UpdateBarUI();

        Debug.Log("공격받는 적 이름: " + this.name + " / 인스턴스ID: " + this.GetInstanceID());
        Debug.Log($"{statData.unitName} 이(가) {damage} 데미지를 받음. 현재 HP: {statData.currentHp}");

        GameManager.Instance.CheckBattleResult();
    }

    // 방어도 추가 (최대 방어도까지)
    public virtual void AddShield(int amount)
    {
        int shieldBefore = statData.currentShield;
        statData.currentShield += amount;
        if (statData.currentShield > statData.maxShield) statData.currentShield = statData.maxShield;

        BattleLog.Add($"{LogName} 방어도 +{statData.currentShield - shieldBefore}", BattleLog.Kind.Shield);
        if (statData.currentShield > shieldBefore) OnCombatNumber?.Invoke(this, NumberKind.Shield, statData.currentShield - shieldBefore);

        UpdateBarUI();
        Debug.Log($"{statData.unitName} 방어도 {amount} 증가! 현재 쉴드: {statData.currentShield}");
    }

    // 방어도 초기화 (턴이 바뀔 때)
    public void ResetShield()
    {
        if (statData.currentShield > 0)
            BattleLog.Add($"{LogName} 방어도 {statData.currentShield} 사라짐");

        statData.currentShield = 0;
    }

    // 회복 (최대 체력까지)
    public virtual void Heal(int amount)
    {
        int hpBefore = statData.currentHp;
        statData.currentHp += amount;
        if(statData.currentHp > statData.maxHp) statData.currentHp = statData.maxHp;

        BattleLog.Add($"{LogName} 체력 +{statData.currentHp - hpBefore}", BattleLog.Kind.Heal);
        if (statData.currentHp > hpBefore) OnCombatNumber?.Invoke(this, NumberKind.Heal, statData.currentHp - hpBefore);

        UpdateBarUI();
        Debug.Log($"{statData.unitName} 체력 {amount} 회복!");
    }

    // 체력 소모 (독한 카드 등 자해 효과). 방어도를 무시하고 체력에서 바로 깎는다
    public virtual void LoseHp(int amount)
    {
        statData.currentHp = Mathf.Max(0, statData.currentHp - amount);

        if (TryGetComponent<PlayerSpineController>(out var playerSpine))
        {
            playerSpine.StartCoroutine(playerSpine.HurtRoutine());
        }

        BattleLog.Add($"{LogName} 체력 -{amount} (카드 효과)", BattleLog.Kind.HpLoss);
        if (amount > 0) OnCombatNumber?.Invoke(this, NumberKind.HpLoss, amount);

        UpdateBarUI();
        Debug.Log($"{statData.unitName} 체력 {amount} 소모! 현재 HP: {statData.currentHp}");
    }

    // 체력 바 등에 지금 체력 / 방어도를 알림
    protected void UpdateBarUI()
    {
        OnHpChanged?.Invoke(statData.currentHp, statData.maxHp);
        OnShieldChanged?.Invoke(statData.currentShield, statData.maxShield);
    }

    public bool IsDead()
    {
        return statData.currentHp <= 0;
    }

    // 세이브 로드: 저장된 체력 / 방어도로 복원
    public void RestoreState(int hp, int shield)
    {
        statData.currentHp = hp;
        statData.currentShield = shield;

        UpdateBarUI();
    }
}