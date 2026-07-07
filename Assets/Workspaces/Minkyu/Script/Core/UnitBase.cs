using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [Header("기본 스탯")]
    public string unitName;
    public int maxHp = 30;
    public int currentHp;
    public int maxShield = 30;
    public int currentShield;

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    public virtual void TakeDamage(int damage)
    {
        if (currentShield > 0)
        {
            currentShield -= damage;

            if (currentShield < 0) {
                currentHp += currentShield;
            }
        }
        else
        {
            currentHp -= damage;
        }

        if (currentHp < 0) currentHp = 0;
        currentShield = 0;
        Debug.Log($"{unitName} 이(가) {damage} 데미지를 받음. 현재 HP: {currentHp}");
    }

    // 방어 추가
    public virtual void AddShield(int amount)
    {
        currentShield += amount;
        // 쉴드 최대치 제한이 필요하면 아래처럼
        if (currentShield > maxShield) currentShield = maxShield;

        Debug.Log($"{unitName} 방어도 {amount} 증가! 현재 쉴드: {currentShield}");
    }

    //회복 추가
    public virtual void Heal(int amount)
    {
        currentHp += amount;
        if(currentHp > maxHp) currentHp = maxHp;

        Debug.Log($"{unitName} 체력 {amount} 회복!");
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }
}