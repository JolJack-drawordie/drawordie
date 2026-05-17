using TMPro;
using UnityEngine;

public class CardVisual : MonoBehaviour
{
    public CardSO data;

    [Header("카드 내용")]
    public TextMeshProUGUI nameText;  //이름
    public TextMeshProUGUI powerText; //수치

    void Start()
    {
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (data == null) return; 

        nameText.text = data.cardName;
        powerText.text = data.power.ToString();
    }
}
