using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemyTarget : MonoBehaviour
{
    private DiceManager diceManager;

    // 카드 사용(공격/방어 연출) 진행 중인 개수. 진행 중에는 턴 종료를 막는다.
    private static int activeActionCount = 0;
    private int myActionCount = 0;
    public static bool IsActing => activeActionCount > 0;

    private void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(400, 400);

        diceManager = FindFirstObjectByType<DiceManager>();
    }

    private void OnDestroy()
    {
        // 연출 도중 파괴되면 카운트가 남지 않도록 정리
        activeActionCount -= myActionCount;
        myActionCount = 0;
    }

    public void ReceiveCard(CardUI card)
    {
        // 플레이어 턴이 아니면(턴 종료 처리 중 등) 카드 사용 불가
        if (GameManager.Instance != null && GameManager.Instance.currentState != BattleState.PlayerTurn)
        {
            card.transform.SetParent(DataManager.Instance.handArea);
            DataManager.Instance.RearrangeHand();
            return;
        }

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
        activeActionCount++;
        myActionCount++;

        // 연출 중 카드가 다시 드래그되지 않도록 막음
        CardDraggable drag = card.GetComponent<CardDraggable>();
        if (drag != null) drag.enabled = false;

        // 연출 대기 중 카드 오브젝트가 파괴될 수 있으므로 값을 미리 저장
        int damage = card.Damage;
        int shield = card.Shield;
        int heal = card.Heal;
        GameObject cardObj = card.gameObject;

        if (card.GerundId != 0 && SkillEffectManager.Instance != null)
        {
            Vector3 effectPosition = card.Damage > 0 || GameManager.Instance.Player == null
                ? transform.position
                : GameManager.Instance.Player.transform.position;
            SkillEffectManager.Instance.PlaySkillEffect(card.AdjectiveId, card.GerundId, effectPosition);
        }

        if(damage > 0)
        {
            if (PlayerController.Instance != null)
                yield return StartCoroutine(PlayerController.Instance.AttackRoutine());
            GameManager.Instance.Enemy.TakeDamage(damage);
        }

        if(shield > 0)
        {
           GameManager.Instance.Player.AddShield(shield);
        }
        
        if(heal > 0)
        {
            GameManager.Instance.Player.Heal(heal);
        }

        GameManager.Instance.CheckBattleResult();

        DataManager.Instance.RearrangeHand();
        if (cardObj != null)
            DataManager.Instance.DiscardCard(cardObj);

        activeActionCount--;
        myActionCount--;

        if (GameManager.Instance.isGameOver)
        {
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
                uiManager.ShowResult(GameManager.Instance.currentState == BattleState.Victory);
        }
    }
}
