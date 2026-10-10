using TMPro;
using UnityEngine;

public class CombinationCardUI : MonoBehaviour
{
    public Combination data;

    [Header("카드 내용")]
    public TMP_Text nameText;  //이름
    public TMP_Text costText; //코스트
    public TMP_Text descText; //내용

    public void SetData(Combination newData)
    {
        this.data = newData;
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (data == null)
        {
            Debug.LogWarning("데이터가 없습니다!");
            return;
        }

        // 연결된 UI가 있는지 한 번 더 확인 (방어 코드)
        if (nameText == null || costText == null || descText == null) return;

        nameText.text = data.skillName;
        costText.text = data.finalCost.ToString();
        descText.text = data.description;
    }
}
