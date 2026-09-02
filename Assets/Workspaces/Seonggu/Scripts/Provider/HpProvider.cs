using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class HpProvider : MonoBehaviour, IValueProvider
{
    private UnitBase _targetUnit;
    public string text => "HP";
    public void SetTarget(UnitBase unit) => _targetUnit = unit;

    public UnitBase GetTarget()
    {
        return _targetUnit;
    }

    public float GetCurrentValue(UnitBase unit)
    {
        // 유닛이 없거나, 스탯 데이터가 아직 안 들어왔으면 에러 내지 말고 0 리턴!
        if (unit == null || unit.statData == null) return 0f;

        return unit.statData.currentHp; // 기존 코드
    }
    public float GetMaxValue(UnitBase unit)
    {
        if (unit == null || unit.statData == null) return 0f;

        return unit.statData.maxHp; // 기존 코드
    }
}
