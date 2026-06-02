using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class EnemyTarget : MonoBehaviour
{
    private void Start()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(400, 400); // 공격하기 쉽게 큼지막한 과녁 생성!
    }

    public void ReceiveCard(CardUI card)
    {
        DiceManager diceManager = FindFirstObjectByType<DiceManager>();

        if (diceManager != null && diceManager.CurrentEnergy >= card.Cost)
        {
            diceManager.UseEnergy(card.Cost); 
            GameManager.Instance.enemy.TakeDamage(card.Damage); 
            
            Destroy(card.gameObject);
            DataManager.Instance.RearrangeHand();
        }
        else
        {
            Debug.Log("마나가 부족합니다!");
            card.transform.SetParent(DataManager.Instance.handArea);
            DataManager.Instance.RearrangeHand();
        }
    }
}