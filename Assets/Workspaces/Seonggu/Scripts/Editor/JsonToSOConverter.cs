using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class JsonToSOConverter : MonoBehaviour
{

    [MenuItem("Tools/Convert Cards JSON to SO")]
    public static void Convert()
    {
        string jsonPath = "Assets/Workspaces/Seonggu/Resources/CardData.json";
        TextAsset jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);

        if (jsonFile == null)
        {
            Debug.LogError("JSON 파일을 불러오지 못했습니다.");
            return;
        }

        // 1. JSON 읽기
        CardDataList dataList = JsonUtility.FromJson<CardDataList>(jsonFile.text);

        // 2. 저장할 폴더 체크
        string folderPath = "Assets/Workspaces/Seonggu/Resources/Cards";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 3. 각 데이터를 SO로 변환
        foreach (var data in dataList.cards)
        {
            CardSO newSO = ScriptableObject.CreateInstance<CardSO>();

            // 데이터 복사
            newSO.cardID = data.cardID;
            newSO.cardName = data.cardName;

            CardCategory cate = (CardCategory)System.Enum.Parse(typeof(CardCategory), data.category);
            newSO.category = cate;
            newSO.isAlltarget = data.isAlltarget;

            newSO.effects = new List<EffectData>();

            foreach (var eJson in data.effects)
            {
                // string을 Enum으로 변환
                EffectType eType = (EffectType)System.Enum.Parse(typeof(EffectType), eJson.type);

                EffectData newEffect = new EffectData
                {
                    type = eType,
                    effectPower = eJson.effectPower,
                    duration = eJson.duration
                };

                // 하나씩 리스트에 추가
                newSO.effects.Add(newEffect);
            }

            newSO.cardIcon = data.cardIcon;

            newSO.power = data.power;
            newSO.description = data.description;


            // 파일로 저장 (파일명: ID_이름.asset)
            string assetPath = $"{folderPath}/Card_{data.cardID}.asset";
            AssetDatabase.CreateAsset(newSO, assetPath);

        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("SO 변환 완료! Resources/Cards 폴더에 생성");
    }

}
