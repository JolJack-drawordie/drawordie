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
    public float GetCurrentValue(UnitBase unit)
    {
        // 유닛이 없거나, 스탯 데이터가 아직 안 들어왔으면 에러 내지 말고 0 리턴!
        // 전투 유닛이 없는 씬(휴식 등)에서는 HpProvider처럼 StatManager 플레이어 스탯 사용
        if (unit == null) unit = _targetUnit;
        if (unit == null) return PlayerStat != null ? PlayerStat.currentShield : 0f;
        if (unit.statData == null) return 0f;

        return unit.statData.currentShield; // 기존 코드
    }
    public float GetMaxValue(UnitBase unit)
    {
        if (unit == null) unit = _targetUnit;
        if (unit == null) return PlayerStat != null ? PlayerStat.maxShield : 0f;
        if (unit.statData == null) return 0f;

        return unit.statData.maxShield; // 기존 코드
    }

    private static UnitStatData PlayerStat =>
        StatManager.Instance != null ? StatManager.Instance.runtimePlayerStat : null;
}
