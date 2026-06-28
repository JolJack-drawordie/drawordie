using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemyTarget : MonoBehaviour
{
    public PlayerController playerController;

    private DiceManager diceManager;

    private void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(400, 400);

        diceManager = FindFirstObjectByType<DiceManager>();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    public void ReceiveCard(CardUI card)
    {
        if (diceManager != null && diceManager.CurrentEnergy >= card.Cost)
        {
            diceManager.UseEnergy(card.Cost);
            StartCoroutine(ActionSequence(card));
        }
        else
        {
            Debug.Log("마나가 부족합니다!");
            card.transform.SetParent(DataManager.Instance.handArea);
            DataManager.Instance.RearrangeHand();
        }
    }

    // 기존 AttackSequence에서 ActionSequence로 변경
    // 방어 행동 추가
    private IEnumerator ActionSequence(CardUI card)
    {
        if(card.Damage > 0)
        {
            if (playerController != null)
                yield return StartCoroutine(playerController.AttackRoutine());
            GameManager.Instance.enemy.TakeDamage(card.Damage);
        }

        if(card.Shield > 0)
        {
           GameManager.Instance.player.AddShield(card.Shield);
        }

        GameManager.Instance.CheckBattleResult();

        Destroy(card.gameObject);
        DataManager.Instance.RearrangeHand();

        if (GameManager.Instance.isGameOver)
        {
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
                uiManager.ShowResult(GameManager.Instance.currentState == BattleState.Victory);
        }
    }
}
