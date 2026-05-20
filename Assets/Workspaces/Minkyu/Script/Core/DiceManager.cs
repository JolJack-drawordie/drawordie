using UnityEngine;

public class DiceManager : MonoBehaviour
{
    [Header("에너지 설정")]
    public int baseEnergy = 3;   // 기본 에너지
    public int diceValue;        // 이번 턴 주사위 값

    public int CurrentEnergy { get; private set; }

    public int RollDice()
    {
        diceValue = Random.Range(1, 7); // 1~6

        CurrentEnergy = baseEnergy + diceValue;

        Debug.Log($"Base Energy {baseEnergy} + Dice {diceValue} = Turn Energy: {CurrentEnergy}");

        return CurrentEnergy;
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