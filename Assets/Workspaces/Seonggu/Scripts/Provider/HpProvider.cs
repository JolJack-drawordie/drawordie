using UnityEngine;

public class HpProvider : MonoBehaviour, IValueProvider
{
    private UnitBase targetUnit;
    public string text => "HP";

    public void SetTarget(UnitBase unit)
    {
        targetUnit = unit;
    }

    public UnitBase GetTarget()
    {
        // 휴식 씬 등에서는 null을 그대로 반환해도 무방합니다.
        return targetUnit;
    }

    public float GetCurrentValue(UnitBase unit)
    {
        // 전달받은 유닛이나 내부 targetUnit이 없다면 StatManager 데이터를 직접 반환
        UnitBase activeUnit = unit != null ? unit : targetUnit;

        if (activeUnit == null)
        {
            if (StatManager.Instance != null && StatManager.Instance.runtimePlayerStat != null)
            {
                return StatManager.Instance.runtimePlayerStat.currentHp;
            }
            return 0f;
        }
        return activeUnit.statData.currentHp;
    }

    public float GetMaxValue(UnitBase unit)
    {
        UnitBase activeUnit = unit != null ? unit : targetUnit;

        if (activeUnit == null)
        {
            if (StatManager.Instance != null && StatManager.Instance.runtimePlayerStat != null)
            {
                return StatManager.Instance.runtimePlayerStat.maxHp;
            }
            return 100f;
        }

        return activeUnit.statData.maxHp;
    }
}