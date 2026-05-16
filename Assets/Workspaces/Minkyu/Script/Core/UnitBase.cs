using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [Header("기본 스탯")]
    public string unitName;
    public int maxHp = 30;
    public int currentHp;

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    public virtual void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp < 0)
            currentHp = 0;

        Debug.Log($"{unitName} 이(가) {damage} 데미지를 받음. 현재 HP: {currentHp}");
    }

    public bool IsDead()
    {
        return currentHp <= 0;
    }
}