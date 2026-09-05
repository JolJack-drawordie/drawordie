using UnityEngine;

public class PlayerUnit : UnitBase
{
    public int attackPower = 6;

    private void Start()
    {
        // ⭐️ 씬에 생성되자마자 GameManager가 있는지 확인하고 자기 자신을 찔러넣음
        //if (GameManager.Instance != null)
        //{
        //    GameManager.Instance.RegisterPlayer(this);
        //}

        //if (UIManager.Instance != null)
        //{
        //    UIManager.Instance.LinkUnitToUI(this);
        //}
    }
    public void Attack(EnemyUnit target)
    {
        if (target == null) return;

        Debug.Log("플레이어 공격!");
        target.TakeDamage(attackPower);
    }
}