using UnityEngine;

public class ShieldProvider : MonoBehaviour, IValueProvider
{
    private UnitBase _targetUnit;
    public string text => "Shield";
    public void SetTarget(UnitBase unit) => _targetUnit = unit;
    public UnitBase GetTarget()
    {
        return _targetUnit;
    }
    public float GetCurrentValue(UnitBase unit) => unit.currentShield;
    public float GetMaxValue(UnitBase unit) => unit.maxShield;
}
