using UnityEngine;

public class DiceManager : MonoBehaviour
{
    public int CurrentEnergy { get; private set; }

    public int RollDice()
    {
        CurrentEnergy = Random.Range(1, 7); // 1~6
        Debug.Log($"주사위를 굴렸습니다. 이번 턴 에너지: {CurrentEnergy}");
        return CurrentEnergy;
    }

    public void UseEnergy(int amount)
    {
        CurrentEnergy -= amount;
        if (CurrentEnergy < 0)
            CurrentEnergy = 0;

        Debug.Log($"에너지 {amount} 사용. 남은 에너지: {CurrentEnergy}");
    }

    public bool HasEnoughEnergy(int cost)
    {
        return CurrentEnergy >= cost;
    }
}