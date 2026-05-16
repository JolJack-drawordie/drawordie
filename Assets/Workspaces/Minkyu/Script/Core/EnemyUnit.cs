using UnityEngine;

public class EnemyUnit : UnitBase
{
    public int attackPower = 4;

    public void Attack(PlayerUnit target)
    {
        if (target == null) return;

        Debug.Log("적 공격!");
        target.TakeDamage(attackPower);
    }
}