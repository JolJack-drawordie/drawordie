using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitStat", menuName = "Battle/Unit Stat Data")]
public class UnitStatData : ScriptableObject
{
    [Header("기본 정보")]
    public string unitName;
    public int maxHp;
    public int currentHp;
    public int maxShield;
    public int currentShield;

    //[Header("추가 스탯 (필요시 확장)")]
    //public int speed;

    // 런타임 복제본이 만들어질 때 체력을 최대 체력으로 초기화하는 용도
    public void ResetStat()
    {
        currentHp = maxHp;
        currentShield = 0;
    }
}