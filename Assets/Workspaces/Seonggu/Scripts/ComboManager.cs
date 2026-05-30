using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;

    public CardSlot adjSlot; 
    public CardSlot gerSlot; 
    public GameObject slotPanel; // 💡 Canvas에 있는 SlotPanel 오브젝트를 인스펙터에서 연결해주세요!

    private CombinationList ComboList;
    private Dictionary<string, Combination> comboTable = new Dictionary<string, Combination>();

    void Awake() { Instance = this; }

    void Start()
    {
        HideSlots();
        StartCoroutine(LoadCombinations());
    }

    public void ShowSlots() { if (slotPanel != null) slotPanel.SetActive(true); }
    public void HideSlots() { if (slotPanel != null) slotPanel.SetActive(false); }

    public void OnSlotUpdated()
    {
        if (adjSlot.isOccupied && gerSlot.isOccupied)
        {
            TryCombination();
        }
    }

    void TryCombination()
    {
        int adjCardID = adjSlot.GetCardID();
        int gerCardID = gerSlot.GetCardID();

        Combination result = Combine(adjCardID, gerCardID);

        if (result != null)
        {
            // ⭐ 기능 개선 1: 완성된 카드를 내 패(Hand)로 보냅니다!
            DataManager.Instance.AddSynergyCardToHand(result);

            // ⭐ 기능 개선 2: 슬롯에 남아있던 재료 카드 2장을 완전히 파괴(삭제)합니다!
            if(adjSlot.currentCard != null) Destroy(adjSlot.currentCard);
            if(gerSlot.currentCard != null) Destroy(gerSlot.currentCard);
            
            adjSlot.RemoveCard();
            gerSlot.RemoveCard();

            // 조합이 끝났으니 슬롯 패널을 숨깁니다.
            HideSlots();
        }
    }

    public Combination Combine(int slotAdjID, int slotGerID)
    {
        string searchKey = $"{slotAdjID}_{slotGerID}";
        if (comboTable.TryGetValue(searchKey, out Combination result)) return result;
        return null;
    }

    IEnumerator LoadCombinations()
    {
        string url = "http://localhost:8080/api/game/load-combinations";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                ComboList = JsonUtility.FromJson<CombinationList>(json);
                comboTable.Clear();
                foreach (Combination combo in ComboList.combinations)
                {
                    comboTable[combo.combinationId] = combo;
                }
            }
        }
    }
}