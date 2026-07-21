using UnityEngine;

public class EnemyUnit : UnitBase
{
    public int attackPower = 4;

    public void Start()
    {
        Debug.Log($"[생성됨] 적 이름: {gameObject.name}, 인스턴스ID: {GetInstanceID()}");
        UIManager.Instance.LinkUnitToUI(this);
    }

    public void Attack(PlayerUnit target)
    {
        if (target == null) return;

        Debug.Log("적 공격!");
        target.TakeDamage(attackPower);
    }
}