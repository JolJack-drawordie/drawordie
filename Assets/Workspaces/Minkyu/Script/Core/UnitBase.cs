using System;
using Unity.VisualScripting;
using UnityEngine;

public class UnitBase : MonoBehaviour
{
    public UnitStatData statData { get; private set; }

    //public AnimatedBar hpBar;
    //public AnimatedBar shieldBar;

    // 체력과 쉴드 변경을 세상에 알릴 이벤트 추가 (기존 로직과 충돌 안 함)
    public event Action<int, int> OnHpChanged;
    public event Action<int, int> OnShieldChanged;

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

    public virtual void TakeDamage(int damage)
    {
        if (statData.currentShield > 0)
        {
            int tempDamage = damage;
            damage = Mathf.Max(0, damage - statData.currentShield);
            statData.currentShield = Mathf.Max(0, statData.currentShield - tempDamage);
        }
        statData.currentHp -= damage;

        if (statData.currentHp < 0) statData.currentHp = 0;

        UpdateBarUI();
        Debug.Log("공격받는 적 이름: " + this.name + " / 인스턴스ID: " + this.GetInstanceID());
        Debug.Log($"{statData.unitName} 이(가) {damage} 데미지를 받음. 현재 HP: {statData.currentHp}");
    }

    // 방어 추가
    public virtual void AddShield(int amount)
    {
        statData.currentShield += amount;
        // 쉴드 최대치 제한이 필요하면 아래처럼
        if (statData.currentShield > statData.maxShield) statData.currentShield = statData.maxShield;

        UpdateBarUI();
        Debug.Log($"{statData.unitName} 방어도 {amount} 증가! 현재 쉴드: {statData.currentShield}");
    }

    public void ResetShield()
    {
        statData.currentShield = 0;
    }

    //회복 추가
    public virtual void Heal(int amount)
    {
        statData.currentHp += amount;
        if(statData.currentHp > statData.maxHp) statData.currentHp = statData.maxHp;

        UpdateBarUI();
        Debug.Log($"{statData.unitName} 체력 {amount} 회복!");
    }

    protected void UpdateBarUI()
    {
        //if (hpBar != null) hpBar.SetValue(statData.currentHp);
        //if (shieldBar != null) shieldBar.SetValue(statData.currentShield);

        // 이벤트 발행 (나중에 AnimatedBar가 이걸 구독하게 만들 예정)
        OnHpChanged?.Invoke(statData.currentHp, statData.maxHp);
        OnShieldChanged?.Invoke(statData.currentShield, statData.maxShield);
    }

    public bool IsDead()
    {
        return statData.currentHp <= 0;
    }
}