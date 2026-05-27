using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ComboManager : MonoBehaviour
{
    // 싱글톤 (어디서든 ComboManager.Instance로 접근 가능하게)
    public static ComboManager Instance;

    public CardSlot adjSlot; // 왼쪽 슬롯
    public CardSlot gerSlot; // 오른쪽 슬롯
    //public CardCombiner combiner; // 조합 로직 스크립트

    [Header("결과 카드 생성 관리")]
    public GameObject cardPrefab;
    public Transform resultParent;
    public Transform spawnPoint;


    CombinationList ComboList;
    private Dictionary<string, Combination> comboTable = new Dictionary<string, Combination>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(LoadCombinations());
    }

    // 1. 슬롯에 카드가 놓일 때마다 이 함수를 부를 겁니다.
    public void OnSlotUpdated()
    {
        // 두 슬롯에 데이터가 다 있는지 확인
        if (adjSlot.currentCard != null && gerSlot.currentCard != null)
        {
            Debug.Log("재료 확인 완료! 조합을 시작합니다.");
            TryCombination();
        }
    }

    // 2. 카드 조합
    void TryCombination()
    {
        int adjCardID = adjSlot.GetCardID();
        int gerCardID = gerSlot.GetCardID();
        Debug.Log("조합 시도");

        if (adjCardID <= 0 || gerCardID <= 0) return;

        // Combiner에게 조합 결과 요청
        Combination result = Combine(adjCardID, gerCardID);

        if (result != null)
        {
            Debug.Log("조합 성공! 결과물: " + result.skillName);
            SpawnResult(result);
        }
        else
        {
            Debug.Log("조합 실패: 일치하는 레시피가 없습니다.");
        }
    }

    // 3. 조합된 카드 생성
    void SpawnResult(Combination resultData)
    {
        GameObject newCard = Instantiate(cardPrefab, spawnPoint.position, Quaternion.identity, resultParent);

        newCard.transform.localScale = Vector3.one;

        CombinationCardUI ui = newCard.GetComponent<CombinationCardUI>();
        if (ui != null)
        {
            ui.SetData(resultData);
            ui.UpdateVisual();
        }

        Debug.Log($"축하합니다! {resultData.skillName} 카드가 생성되었습니다.");
    }

    public Combination Combine(int slotAdjID, int slotGerID)
    {
        if (slotAdjID <= 0 || slotGerID <= 0) return null;

        // 슬롯 카드 ID 조합으로 검색용 문자열 키 생성 ("1_2")
        string searchKey = $"{slotAdjID}_{slotGerID}";

        Debug.Log($"[조합 시도] 검색 키: {searchKey}");

        if (comboTable.TryGetValue(searchKey, out Combination result))
        {
            return result;
        }

        return null; // 일치하는 조합법 없음
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
                Debug.Log($"[성공] 총 {ComboList.combinations.Count}개의 결과 카드 로드 완료!");

                //딕셔너리에 결과 카드 데이터 채우기
                comboTable.Clear();

                foreach (Combination combo in ComboList.combinations)
                {
                    string key = combo.combinationId;
                    comboTable[key] = combo;
                }
            }
            else
            {
                Debug.LogError("서버에서 카드를 가져오지 못했습니다: " + www.error);
            }
        }
    }
}