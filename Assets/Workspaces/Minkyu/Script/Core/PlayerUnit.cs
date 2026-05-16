using UnityEngine;

public class PlayerUnit : UnitBase
{
    public int attackPower = 6;

    public void Attack(EnemyUnit target)
    {
        if (target == null) return;

        Debug.Log("플레이어 공격!");
        target.TakeDamage(attackPower);
    }
}