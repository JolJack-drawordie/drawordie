// 바(AnimatedBar)에 보여 줄 값을 주는 쪽 (체력 / 방어도)
public interface IValueProvider
{
    string text { get; } // 글자 앞머리 (예: "HP")
    void SetTarget(UnitBase unit); // 값을 읽을 유닛 연결
    UnitBase GetTarget();
    float GetCurrentValue(UnitBase unit);
    float GetMaxValue(UnitBase unit);
}
