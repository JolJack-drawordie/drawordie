using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimatedBar : MonoBehaviour
{
    [Header("UI Components")]
    public Slider bar;
    public TextMeshProUGUI textLabel;

    [Header("애니메이션 설정")]
    public float speed = 2f; // 속도 조절 가능

    private string _prefix;
    private IValueProvider _provider;
    private UnitBase _targetUnit;

    private float _target = 0f;
    private bool _isAnimating = false;

    

    void Awake()
    {
        bar = GetComponentInChildren<Slider>();
        textLabel = GetComponentInChildren<TextMeshProUGUI>();
    }

    void Update()
    {
        Debug.Log("Update 실행 중");
        Debug.Log($"[Bar Debug] {gameObject.name} | Target: {_targetUnit} | Provider: {_provider}");

        if (_targetUnit == null || _provider == null) return;

        // 1. Provider를 통해 현재 값과 최대값 가져오기
        float current = _provider.GetCurrentValue(_targetUnit);
        float max = _provider.GetMaxValue(_targetUnit);
        float percent = (max > 0) ? current / max : 0f;

        Debug.Log($"[Bar Log] Current: {current}, Max: {max}, SliderValue: {current / max}");

        // 2. 만약 계산한 값(percent)과 현재 목표(_target)가 다르면 애니메이션 발동!
        if (!Mathf.Approximately(_target, percent))
        {
            SetValue(percent);
        }

        // 3. 텍스트 갱신 (마지막에 실제 현재 수치로 업데이트)
        if (textLabel != null)
        {
            textLabel.text = $"{_prefix}{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        // 4. 슬라이더 애니메이션 로직 실행
        if (!_isAnimating) return;
        if (bar == null) return;

        bar.value = Mathf.MoveTowards(bar.value, _target, Time.deltaTime * speed);

        if (Mathf.Approximately(bar.value, _target))
        {
            bar.value = _target;
            _isAnimating = false;
        }
    }

    // 외부에서 호출할 함수
    public void SetValue(float newValue)
    {
        _target = newValue;
        _isAnimating = true;
    }

    public void SetProvider(IValueProvider provider)
    {
        _provider = provider;
        _targetUnit = provider.GetTarget();
        _prefix = provider.text + " ";
        if (_targetUnit != null && _provider != null)
        {
            float current = _provider.GetCurrentValue(_targetUnit);
            float max = _provider.GetMaxValue(_targetUnit);
            float percent = (max > 0) ? current / max : 0f;

            _target = percent;
            if (bar != null) bar.value = percent;
        }
    }
}