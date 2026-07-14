using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [Header("기본 스탯")]
    public string unitName;
    public int maxHp = 30;
    public int currentHp;
    public int maxShield = 30;
    public int currentShield;

    public AnimatedBar hpBar;
    public AnimatedBar shieldBar;

    protected virtual void Awake()
    {
        currentHp = maxHp;
        hpBar = GetComponent<AnimatedBar>();
        shieldBar = GetComponent<AnimatedBar>();    
    }

    public virtual void TakeDamage(int damage)
    {
        if (currentShield > 0)
        {
            int tempDamage = damage;
            damage = Mathf.Max(0, damage - currentShield);
            currentShield = Mathf.Max(0, currentShield - tempDamage);
        }
        currentHp -= damage;

        if (currentHp < 0) currentHp = 0;

        UpdateBarUI();
        Debug.Log("공격받는 적 이름: " + this.name + " / 인스턴스ID: " + this.GetInstanceID());
        Debug.Log($"{unitName} 이(가) {damage} 데미지를 받음. 현재 HP: {currentHp}");
    }

    // 방어 추가
    public virtual void AddShield(int amount)
    {
        currentShield += amount;
        // 쉴드 최대치 제한이 필요하면 아래처럼
        if (currentShield > maxShield) currentShield = maxShield;

        UpdateBarUI();
        Debug.Log($"{unitName} 방어도 {amount} 증가! 현재 쉴드: {currentShield}");
    }

    public void ResetShield()
    {
        currentShield = 0;
    }

    //회복 추가
    public virtual void Heal(int amount)
    {
        currentHp += amount;
        if(currentHp > maxHp) currentHp = maxHp;

        UpdateBarUI();
        Debug.Log($"{unitName} 체력 {amount} 회복!");
    }

    protected void UpdateBarUI()
    {
        if (hpBar != null) hpBar.SetValue(currentHp);
        if (shieldBar != null) shieldBar.SetValue(currentShield);
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }
}