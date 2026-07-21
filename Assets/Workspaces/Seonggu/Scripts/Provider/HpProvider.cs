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

    public float GetCurrentValue(UnitBase unit) => unit.currentHp;
    public float GetMaxValue(UnitBase unit) => unit.maxHp;
}
