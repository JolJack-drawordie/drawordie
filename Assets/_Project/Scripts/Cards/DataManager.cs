using UnityEngine;
using System.IO;
using System.Collections; // ⭐ 코루틴 사용을 위해 반드시 필요!
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class DataManager : MonoBehaviour
{
    [Header("데이터베이스")]
    public AdjectiveList adjDB; 
    public VerbList verbDB;      

    [Header("전투 시스템")]
    public int currentMana;
    public int diceResult;

    [Header("카드 패(Hand) 시스템")]
    public GameObject cardPrefab;
    public Transform handArea;

    void Start() { LoadAllData(); }

    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame) { RollDiceAndStartTurn(); }
    }

    public void LoadAllData()
    {
        string baseDir = Path.Combine(Application.dataPath, "_Project", "Data");
        
        string adjPath = Path.Combine(baseDir, "Adjectives.json");
        if (File.Exists(adjPath)) adjDB = JsonUtility.FromJson<AdjectiveList>(File.ReadAllText(adjPath));

        string verbPath = Path.Combine(baseDir, "Verbs.json");
        if (File.Exists(verbPath)) verbDB = JsonUtility.FromJson<VerbList>(File.ReadAllText(verbPath));
    }

    public void RollDiceAndStartTurn()
    {
        diceResult = Random.Range(1, 7);
        currentMana = diceResult;
        Debug.Log($"🎲 주사위: {diceResult} (마나: {currentMana})");

        // ⭐ 코루틴을 실행할 때는 StartCoroutine()을 써야 합니다!
        StartCoroutine(DrawCardsRoutine(5));
    }

    // ⭐ 핵심: 0.2초씩 쉬면서 카드를 하나씩 생성하는 코루틴 함수
    IEnumerator DrawCardsRoutine(int count)
    {
        // 1. 기존 카드 치우기
        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"<color=yellow>--- {count}장의 카드를 순차적으로 뽑습니다 ---</color>");

        float cardSpacing = 130f;    
        float heightSpacing = 15f;   
        float angleSpacing = -6f;    

        for (int i = 0; i < count; i++)
        {
            int cardType = Random.Range(0, 2); 
            string cName = "", cCost = "", cDesc = "";

            if (cardType == 0) // 형용사
            {
                var adj = adjDB.adjectives[Random.Range(0, adjDB.adjectives.Count)];
                cName = adj.name;
                // ⭐ 기호 붙이는 복잡한 코드를 지우고, 일반 숫자처럼 변환합니다.
                cCost = adj.cost.ToString(); 
                cDesc = adj.desc;
            }
            else // 동사
            {
                var verb = verbDB.verbs[Random.Range(0, verbDB.verbs.Count)];
                cName = verb.name;
                cCost = verb.baseCost.ToString();
                cDesc = verb.desc;
            }

            GameObject newCard = Instantiate(cardPrefab, handArea);
            
            // 생성되자마자 덱 위치(중앙 상단)로 이동
            RectTransform rect = newCard.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(0, 600f, 0); 

            float centerOffset = i - (count - 1) / 2f; 
            float xPos = centerOffset * cardSpacing;
            float yPos = -Mathf.Abs(centerOffset) * heightSpacing; 
            float zRot = centerOffset * angleSpacing;              

            CardUI ui = newCard.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.Setup(cName, cCost, cDesc);
                ui.SetTransform(new Vector3(xPos, yPos, 0), Quaternion.Euler(0, 0, zRot), i);
            }

            // ⭐ 마법의 문장: 여기서 0.15초 동안 멈췄다가 다음 카드를 생성합니다!
            // 이 숫자를 조절해서 드로우 속도를 바꿀 수 있습니다.
            yield return new WaitForSeconds(0.15f); 
        }
    }
}