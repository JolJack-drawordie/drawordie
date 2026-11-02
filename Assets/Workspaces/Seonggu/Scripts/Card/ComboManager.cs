using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// 카드 조합: 형용사 칸 + 동명사 칸에 카드를 놓으면 결과를 미리 보여 주고, 조합 버튼을 누르면 조합 카드를 손패에 만든다.
// 조합표는 서버에서 받는다 (key: "형용사id_동명사id").
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;

    public CardSlot adjSlot;
    public CardSlot gerSlot;
    public GameObject slotPanel; // 조합 슬롯 묶음 (형용사 카드를 끌 때만 보임)

    private CombinationList ComboList;
    private Dictionary<string, Combination> comboTable = new Dictionary<string, Combination>();

    // 서버 조합표 요청이 끝났는지 (성공 / 실패 상관없이). 세이브의 조합 카드 복원은 이 뒤에 해야 함
    public bool IsLoadFinished { get; private set; }

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

    // 두 칸이 다 차면 결과 미리보기, 아니면 미리보기 지우기
    public void OnSlotUpdated()
    {
        if (adjSlot.isOccupied && gerSlot.isOccupied)
        {
            TryCombination();
        }
        else{
            DataManager.Instance.ClearPreview();
        }
    }

    // 지금 두 칸의 카드로 조합 결과를 찾아 미리보기
    void TryCombination()
    {
        int adjCardID = adjSlot.GetCardID();
        int gerCardID = gerSlot.GetCardID();

        Combination result = Combine(adjCardID, gerCardID);

        if (result != null)
        {
            DataManager.Instance.CombinationPreview(result);
        }
    }

    // 조합 버튼: 조합 카드를 손패에 만들고, 재료 두 장은 덱에서 소모하고 칸을 비운다
    public void OnClickCombination()
    {
        DataManager.Instance.ClearPreview();

        int adjCardID = adjSlot.GetCardID();

        int gerCardID = gerSlot.GetCardID();

        Combination result = Combine(adjCardID, gerCardID);

        if (result != null)
        {
            // 조합 카드 생성 후 손패에 추가
            DataManager.Instance.AddSynergyCardToHand(result);

            // 유물 '학자의 돋보기' 발동
            if (RelicManager.Instance != null)
            {
                RelicManager.Instance.OnCardCombination();
            }

            // 조합에 쓴 재료 카드 소모
            DeckManager.Instance.UseCardsForCombination();

            if (adjSlot.currentCard != null) Destroy(adjSlot.currentCard);
            if (gerSlot.currentCard != null) Destroy(gerSlot.currentCard);

            adjSlot.RemoveCard();
            gerSlot.RemoveCard();
            HideSlots();
        }
    }

    // 형용사 id + 동명사 id로 조합 결과 찾기 (없으면 null)
    public Combination Combine(int slotAdjID, int slotGerID)
    {
        string searchKey = $"{slotAdjID}_{slotGerID}";
        if (comboTable.TryGetValue(searchKey, out Combination result)) return result;
        return null;
    }

    // 서버에서 조합표 받기
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

                if (ComboList?.combinations != null)
                {
                    foreach (Combination combo in ComboList.combinations)
                        comboTable[$"{combo.adjectiveId}_{combo.gerundId}"] = combo;
                }
            }
        }

        IsLoadFinished = true;
    }
}
