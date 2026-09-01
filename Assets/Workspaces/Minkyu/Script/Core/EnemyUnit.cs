using UnityEngine;

public class EnemyUnit : UnitBase
{
    public int attackPower = 4;

    public void Start()
    {
        // ⭐️ 씬에 생성되자마자 GameManager가 있는지 확인하고 자기 자신을 찔러넣음
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemy(this);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.LinkUnitToUI(this);
        }

        Debug.Log($"[생성됨] 적 이름: {gameObject.name}, 인스턴스ID: {GetInstanceID()}");
    }

    public void Attack(PlayerUnit target)
    {
        if (target == null) return;

        Debug.Log("적 공격!");
        target.TakeDamage(attackPower);
    }
}