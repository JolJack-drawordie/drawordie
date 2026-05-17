using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CardCombiner : MonoBehaviour
{
    public static CardCombiner Instance { get; private set; }

    private Dictionary<string, CardSO> recipeTable = new Dictionary<string, CardSO>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 게임 켜지자마자 JSON 로드해서 딕셔너리 장전하기
        LoadRecipeData();
    }

    private void LoadRecipeData()
    {
        // 1. Resources 폴더에서 레시피 JSON 파일 '바로' 가져오기
        TextAsset jsonFile = Resources.Load<TextAsset>("RecipeData");

        if (jsonFile == null)
        {
            Debug.LogError("[CardCombiner] Resources 폴더에서 RecipeData JSON 파일을 찾을 수 없습니다!");
            return;
        }

        // 2. JSON 텍스트를 RecipeDataList 구조체로 파싱
        RecipeDataList dataList = JsonUtility.FromJson<RecipeDataList>(jsonFile.text);

        recipeTable.Clear();

        // 3. 루프를 돌며 딕셔너리 상자에 string ID("1_2")를 Key로 채워넣기
        foreach (var recipe in dataList.recipes)
        {
            string key = recipe.recipeID; // "1_2"

            // 결과물 카드는 원래 하던 대로 Resources/Cards 폴더에서 SO 파일로 땡겨옴
            CardSO resultCard = Resources.Load<CardSO>($"Cards/Card_{recipe.resultCardID}");

            if (resultCard == null)
            {
                Debug.LogWarning($"[CardCombiner] 결과 카드 SO를 찾을 수 없습니다: Cards/Card_{recipe.resultCardID}");
                continue;
            }

            if (!recipeTable.ContainsKey(key))
            {
                recipeTable.Add(key, resultCard);
            }
        }

        Debug.Log($"[CardCombiner] JSON에서 총 {recipeTable.Count}개의 조합법을 딕셔너리에 세팅 완료했습니다.");
    }

    public CardSO Combine(CardSO slotAdj, CardSO slotNoun)
    {
        if (slotAdj == null || slotNoun == null) return null;

        // 슬롯 카드 ID 조합으로 검색용 문자열 키 생성 ("1_2")
        string searchKey = $"{slotAdj.cardID}_{slotNoun.cardID}";

        Debug.Log($"[조합 시도] 검색 키: {searchKey}");

        if (recipeTable.TryGetValue(searchKey, out CardSO result))
        {
            return result;
        }

        return null; // 일치하는 조합법 없음
    }
}
