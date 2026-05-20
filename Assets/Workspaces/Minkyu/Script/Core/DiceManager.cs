using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    [Header("에너지 설정")]
    public int baseEnergy = 3;
    public int diceValue;

    [Header("주사위 UI")]
    public Image diceImage;
    public Sprite[] diceSprites;

    public int CurrentEnergy { get; private set; }

    public int RollDice()
    {
        diceValue = Random.Range(1, 7);

        CurrentEnergy = baseEnergy + diceValue;

        UpdateDiceImage();

        Debug.Log($"Base Energy {baseEnergy} + Dice {diceValue} = Turn Energy: {CurrentEnergy}");

        return CurrentEnergy;
    }

    private void UpdateDiceImage()
    {
        if (diceImage == null)
        {
            Debug.LogWarning("Dice Image가 연결되지 않았습니다.");
            return;
        }

        if (diceSprites == null || diceSprites.Length < 6)
        {
            Debug.LogWarning("주사위 Sprite가 6개 연결되지 않았습니다.");
            return;
        }

        diceImage.sprite = diceSprites[diceValue - 1];
    }

    public void UseEnergy(int amount)
    {
        CurrentEnergy -= amount;

        if (CurrentEnergy < 0)
        {
            CurrentEnergy = 0;
        }

        Debug.Log($"Use Energy {amount}. Remain Energy: {CurrentEnergy}");
    }

    public bool HasEnoughEnergy(int cost)
    {
        return CurrentEnergy >= cost;
    }
}