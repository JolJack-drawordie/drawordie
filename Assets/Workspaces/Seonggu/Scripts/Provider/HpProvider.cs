using UnityEngine;

// 체력 바에 체력 값을 주는 Provider. 연결된 유닛이 없으면(휴식 씬 등) StatManager의 플레이어 체력을 쓴다.
public class HpProvider : MonoBehaviour, IValueProvider
{
    private UnitBase targetUnit;
    public string text => "HP";

    public void SetTarget(UnitBase unit)
    {
        targetUnit = unit;
    }

    // 휴식 씬 등에서는 null일 수 있음
    public UnitBase GetTarget()
    {
        return targetUnit;
    }

    public float GetCurrentValue(UnitBase unit)
    {
        // 받은 유닛이 없으면 연결된 유닛, 그것도 없으면 StatManager의 플레이어 스탯
        UnitBase activeUnit = unit != null ? unit : targetUnit;

        if (activeUnit == null)
        {
            if (StatManager.Instance != null && StatManager.Instance.runtimePlayerStat != null)
            {
                return StatManager.Instance.runtimePlayerStat.currentHp;
            }
            return 0f;
        }
        if (activeUnit.statData == null) return 0f; // 아직 스탯이 들어오기 전
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

        if (activeUnit.statData == null) return 100f;
        return activeUnit.statData.maxHp;
    }
}