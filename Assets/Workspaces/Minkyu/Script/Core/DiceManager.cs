using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    [Header("에너지 설정")]
    public int baseEnergy = 3;
    public int diceValue;
    public int CurrentEnergy { get; private set; }

    [Header("주사위 UI")]
    public Button rollDiceButton; // 버튼 연결
    public GameObject diceImageObject; // 주사위 이미지 부모
    public Image diceImage;
    public Sprite[] diceSprites;

    public bool isRollFinished = false; // 턴매니저 대기용 플래그

    void Start()
    {
        if (diceImageObject != null) diceImageObject.SetActive(false); 
        if (rollDiceButton != null) rollDiceButton.onClick.AddListener(OnClickRollButton);
    }

    public void ShowRollButton()
    {
        isRollFinished = false;
        if (rollDiceButton != null) rollDiceButton.gameObject.SetActive(true);
    }

    private void OnClickRollButton()
    {
        rollDiceButton.gameObject.SetActive(false);
        StartCoroutine(RollDiceRoutine());
    }

    IEnumerator RollDiceRoutine()
    {
        diceImageObject.SetActive(true);
        
        // 주사위 굴러가는 애니메이션
        for(int i = 0; i < 10; i++)
        {
            diceImage.sprite = diceSprites[Random.Range(0, 6)];
            yield return new WaitForSeconds(0.05f);
        }

        diceValue = Random.Range(1, 7);
        CurrentEnergy = baseEnergy + diceValue;
        
        if (diceImage != null && diceSprites.Length >= 6)
            diceImage.sprite = diceSprites[diceValue - 1];

        Debug.Log($"Base Energy {baseEnergy} + Dice {diceValue} = Turn Energy: {CurrentEnergy}");

        yield return new WaitForSeconds(1f);
        diceImageObject.SetActive(false);

        isRollFinished = true; // 주사위가 끝나면 TurnManager가 다음을 진행함!
    }

    public void UseEnergy(int amount)
    {
        CurrentEnergy -= amount;
        if (CurrentEnergy < 0) CurrentEnergy = 0;
    }
}