using UnityEngine;

// 방어도 바에 방어도 값을 주는 Provider. 연결된 유닛이 없으면(휴식 씬 등) StatManager의 플레이어 방어도를 쓴다.
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
        // 받은 유닛이 없으면 연결된 유닛, 그것도 없으면 StatManager의 플레이어 스탯. 스탯이 들어오기 전이면 0
        if (unit == null) unit = _targetUnit;
        if (unit == null) return PlayerStat != null ? PlayerStat.currentShield : 0f;
        if (unit.statData == null) return 0f;

        return unit.statData.currentShield;
    }
    public float GetMaxValue(UnitBase unit)
    {
        if (unit == null) unit = _targetUnit;
        if (unit == null) return PlayerStat != null ? PlayerStat.maxShield : 0f;
        if (unit.statData == null) return 0f;

        return unit.statData.maxShield;
    }

    // 씬 사이에서 유지되는 플레이어 스탯
    private static UnitStatData PlayerStat =>
        StatManager.Instance != null ? StatManager.Instance.runtimePlayerStat : null;
}
