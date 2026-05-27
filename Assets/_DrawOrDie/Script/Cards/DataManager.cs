using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.InputSystem;

public class DataManager : MonoBehaviour
{
    [Header("전투 시스템")]
    public int currentMana;
    public int diceResult;

    [Header("카드 패(Hand) 시스템")]
    public GameObject cardPrefab;
    public Transform handArea;

    void Update()
    {
        // R키를 누르면 주사위를 굴리고 턴을 시작!
        if (Keyboard.current.rKey.wasPressedThisFrame) 
        { 
            RollDiceAndStartTurn(); 
        }
    }

    public void RollDiceAndStartTurn()
    {
        // 1. 주사위 기반 코스트 시스템 작동!
        diceResult = Random.Range(1, 7);
        currentMana = diceResult;
        Debug.Log($"🎲 주사위: {diceResult} (마나: {currentMana})");

        // 2. 서버에 카드 요청 시작
        StartCoroutine(FetchAndDrawCards());
    }

    // 🔥 [추가된 부분] 민규님의 TurnManager가 밖에서 안전하게 누를 수 있는 전용 스위치! 🔥
    public void TriggerCardDraw()
    {
        StartCoroutine(FetchAndDrawCards());
    }
    
    // ⭐ 1단계: 서버에서 카드 5장 받아오기
    IEnumerator FetchAndDrawCards()
    {
        // 기존 카드 싹 치우기
        foreach (Transform child in handArea) Destroy(child.gameObject);

        string url = "http://localhost:8080/api/game/start-cards";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // 서버 JSON을 C# 클래스로 완벽 변환!
                string json = www.downloadHandler.text;
                BattleStartHand hand = JsonUtility.FromJson<BattleStartHand>(json);
                
                // 받아온 카드로 드로우 애니메이션 시작
                StartCoroutine(DrawCardsRoutine(hand));
            }
            else
            {
                Debug.LogError("서버에서 카드를 가져오지 못했습니다: " + www.error);
            }
        }
    }

    // ⭐ 2단계: 받아온 카드로 프리팹 생성 및 연출 (기존 코드 활용)
    IEnumerator DrawCardsRoutine(BattleStartHand hand)
    {
        int totalCards = hand.adjectives.Count + hand.gerunds.Count; // 2 + 3 = 5장
        Debug.Log($"<color=yellow>--- 서버에서 뽑아준 {totalCards}장의 카드를 순차적으로 냅니다 ---</color>");

        float cardSpacing = 130f;    
        float heightSpacing = 15f;   
        float angleSpacing = -6f;    

        // 홍성구 추가 : 카드 id 배열에 담기
        int[] cID = { hand.adjectives[0].id, hand.adjectives[1].id, hand.gerunds[0].id, hand.gerunds[1].id, hand.gerunds[2].id };

        // 카드 데이터를 순서대로 배열에 담기 (형용사 2장 -> 동명사 3장 순서)
        string[] cNames = { hand.adjectives[0].name, hand.adjectives[1].name, hand.gerunds[0].name, hand.gerunds[1].name, hand.gerunds[2].name };
        string[] cCosts = { hand.adjectives[0].costMod.ToString(), hand.adjectives[1].costMod.ToString(), hand.gerunds[0].baseCost.ToString(), hand.gerunds[1].baseCost.ToString(), hand.gerunds[2].baseCost.ToString() };
        string[] cDescs = { hand.adjectives[0].desc, hand.adjectives[1].desc, hand.gerunds[0].desc, hand.gerunds[1].desc, hand.gerunds[2].desc };


        for (int i = 0; i < totalCards; i++)
        {
            GameObject newCard = Instantiate(cardPrefab, handArea);
            
            // 생성되자마자 덱 위치(중앙 상단)로 초기화
            RectTransform rect = newCard.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 600f, 0); 

            float centerOffset = i - (totalCards - 1) / 2f; 
            float xPos = centerOffset * cardSpacing;
            float yPos = -Mathf.Abs(centerOffset) * heightSpacing; 
            float zRot = centerOffset * angleSpacing;              

            CardUI ui = newCard.GetComponent<CardUI>();
            if (ui != null)
            {
                // 배열에 담아둔 데이터로 프리팹 세팅!
                ui.Setup(cID[i], cNames[i], cCosts[i], cDescs[i]); // 홍성구 수정 : 카드 id 추가
                ui.SetTransform(new Vector3(xPos, yPos, 0), Quaternion.Euler(0, 0, zRot), i);
            }

            // 0.15초 대기 꿀맛 연출
            yield return new WaitForSeconds(0.15f); 
        }
    }
}