using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    [Header("전투 시스템")]
    public int currentMana;
    public int diceResult;

    [Header("카드 패(Hand) 시스템")]
    public GameObject cardPrefab;
    public Transform handArea;
    
    // 1920x1080 해상도 기준 부채꼴 레이아웃
    public float cardSpacing = 200f;
    public float heightSpacing = 35f;
    public float angleSpacing = -9f;

    void Awake() { Instance = this; }

    public void TriggerCardDraw(int diceEnergy)
    {
        currentMana = diceEnergy;
        StartCoroutine(FetchAndDrawCards());
    }
    
    public void ClearHand()
    {
        ComboManager.Instance.ClearAllSlots();
        foreach (Transform child in handArea) Destroy(child.gameObject);
    }

    IEnumerator FetchAndDrawCards()
    {
        ClearHand();

        string url = "http://localhost:8080/api/game/start-cards";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                BattleStartHand hand = JsonUtility.FromJson<BattleStartHand>(json);
                StartCoroutine(DrawCardsRoutine(hand));
            }
        }
    }

    IEnumerator DrawCardsRoutine(BattleStartHand hand)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.Setup(hand.adjectives[i].id, hand.adjectives[i].name, hand.adjectives[i].costMod.ToString(), 
                     hand.adjectives[i].desc, CardType.Adjective, hand.adjectives[i].costMod, hand.adjectives[i].dmgMod, hand.adjectives[i].shdMod, hand.adjectives[i].healMod);
            
            RectTransform rect = newCard.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 600f, 0); 
            RearrangeHand();
            yield return new WaitForSeconds(0.15f);
        }
        
        for (int i = 0; i < 3; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            CardUI ui = newCard.GetComponent<CardUI>();
            ui.Setup(hand.gerunds[i].id, hand.gerunds[i].name, hand.gerunds[i].baseCost.ToString(), 
                     hand.gerunds[i].desc, CardType.Gerund, hand.gerunds[i].baseCost, hand.gerunds[i].baseDmg, hand.gerunds[i].baseShd, hand.gerunds[i].baseHeal);
            
            RectTransform rect = newCard.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 600f, 0); 
            RearrangeHand();
            yield return new WaitForSeconds(0.15f);
        }
    }

    public void AddSynergyCardToHand(Combination comboData)
    {
        GameObject newCard = Instantiate(cardPrefab, handArea);
        CardUI ui = newCard.GetComponent<CardUI>();
        ui.Setup(0, comboData.skillName, comboData.finalCost.ToString(), 
                 comboData.description, CardType.Synergy, comboData.finalCost, comboData.finalDamage, comboData.finalShield, comboData.finalHeal);
        RearrangeHand();
    }

    public void RearrangeHand()
    {
        List<CardUI> cardsInHand = new List<CardUI>();
        foreach (Transform child in handArea)
        {
            CardUI ui = child.GetComponent<CardUI>();
            if (ui != null && !ui.isInSlot) cardsInHand.Add(ui);
        }

        int total = cardsInHand.Count;
        for (int i = 0; i < total; i++)
        {
            float centerOffset = i - (total - 1) / 2f; 
            float xPos = centerOffset * cardSpacing;
            float yPos = -Mathf.Abs(centerOffset) * heightSpacing; 
            float zRot = centerOffset * angleSpacing;              

            cardsInHand[i].siblingIndex = i;
            cardsInHand[i].originPos = new Vector3(xPos, yPos, 0);
            cardsInHand[i].originRot = Quaternion.Euler(0, 0, zRot);

            cardsInHand[i].targetPos = cardsInHand[i].originPos;
            cardsInHand[i].targetRot = cardsInHand[i].originRot;
        }
    }
}