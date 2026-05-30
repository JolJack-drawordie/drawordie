using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;

    public CardSlot adjSlot;
    public CardSlot gerSlot;
    public GameObject slotPanel;

    private CombinationList ComboList;
    private Dictionary<string, Combination> comboTable = new Dictionary<string, Combination>();

    void Awake() { Instance = this; }

    void Start()
    {
        adjSlot.slotType = SlotType.Adjective;
        gerSlot.slotType = SlotType.Gerund;

        HideSlots();
        StartCoroutine(LoadCombinations());
    }

    public void ShowSlots() { if (slotPanel != null) slotPanel.SetActive(true); }
    public void HideSlots() { if (slotPanel != null) slotPanel.SetActive(false); }

    public void ClearAllSlots()
    {
        if (adjSlot.currentCard != null) Destroy(adjSlot.currentCard);
        if (gerSlot.currentCard != null) Destroy(gerSlot.currentCard);
        adjSlot.RemoveCard();
        gerSlot.RemoveCard();
        HideSlots();
    }

    public void OnSlotUpdated()
    {
        if (adjSlot.isOccupied && gerSlot.isOccupied)
            TryCombination();
    }

    void TryCombination()
    {
        int adjCardID = adjSlot.GetCardID();
        int gerCardID = gerSlot.GetCardID();

        Combination result = Combine(adjCardID, gerCardID);

        if (result != null)
        {
            DataManager.Instance.AddSynergyCardToHand(result);

            if (adjSlot.currentCard != null) Destroy(adjSlot.currentCard);
            if (gerSlot.currentCard != null) Destroy(gerSlot.currentCard);

            adjSlot.RemoveCard();
            gerSlot.RemoveCard();
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

                if (ComboList?.combinations == null || ComboList.combinations.Count == 0)
                    yield break;

                foreach (Combination combo in ComboList.combinations)
                    comboTable[$"{combo.adjectiveId}_{combo.gerundId}"] = combo;
            }
        }
    }
}
