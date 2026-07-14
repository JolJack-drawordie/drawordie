public interface IValueProvider
{
    string text { get; }
    void SetTarget(UnitBase unit);
    UnitBase GetTarget();
    float GetCurrentValue(UnitBase unit);
    float GetMaxValue(UnitBase unit);
}
