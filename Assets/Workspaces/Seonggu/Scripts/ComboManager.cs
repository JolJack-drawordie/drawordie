using UnityEngine;

public class ComboManager : MonoBehaviour
{
    // 싱글톤 (어디서든 ComboManager.Instance로 접근 가능하게)
    public static ComboManager Instance;

    public CardSlot adjSlot; // 왼쪽 슬롯
    public CardSlot nounSlot; // 오른쪽 슬롯
    public CardCombiner combiner; // 조합 로직 스크립트


    [Header("결과 카드 생성 관리")]
    public GameObject cardPrefab;
    public Transform resultParent;
    public Transform spawnPoint;


    void Awake()
    {
        Instance = this;
    }

    // 1. 슬롯에 카드가 놓일 때마다 이 함수를 부를 겁니다.
    public void OnSlotUpdated()
    {
        // 두 슬롯에 데이터가 다 있는지 확인
        if (adjSlot.currentCard != null && nounSlot.currentCard != null)
        {
            Debug.Log("재료 확인 완료! 조합을 시작합니다.");
            TryCombination();
        }
    }

    // 2. 카드 조합
    void TryCombination()
    {
        CardSO adjCard = adjSlot.GetCardData();
        CardSO nounCard = nounSlot.GetCardData();

        if (adjCard == null || nounCard == null) return;

        // Combiner에게 조합 결과 요청
        CardSO result = combiner.Combine(adjCard, nounCard);

        if (result != null)
        {
            Debug.Log("조합 성공! 결과물: " + result.cardName);
            SpawnResult(result);
        }
        else
        {
            Debug.Log("조합 실패: 일치하는 레시피가 없습니다.");
        }
    }

    // 3. 조합된 카드 생성
    void SpawnResult(CardSO resultData)
    {
        GameObject newCard = Instantiate(cardPrefab, spawnPoint.position, Quaternion.identity, resultParent);

        newCard.transform.localScale = Vector3.one;

        CardVisual visual = newCard.GetComponent<CardVisual>();
        if (visual != null)
        {
            visual.data = resultData;
            visual.UpdateVisual();
        }

        Debug.Log($"축하합니다! {resultData.cardName} 카드가 생성되었습니다.");
    }
}