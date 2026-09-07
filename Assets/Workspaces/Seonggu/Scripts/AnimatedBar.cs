using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimatedBar : MonoBehaviour
{
    [Header("UI Components")]
    public Slider bar;
    public TextMeshProUGUI textLabel;

    [Header("애니메이션 설정")]
    public float speed = 2f; // 슬라이더가 차오르거나 깎이는 속도

    private string _prefix;
    private IValueProvider _provider;
    private UnitBase _targetUnit;

    private float _target = 0f;
    private bool _isAnimating = false;

    void Awake()
    {
        // 인스펙터 칸이 비어있을 때만 스스로 찾도록 안전장치 추가!
        if (bar == null) bar = GetComponent<Slider>();
        if (bar == null) bar = GetComponentInChildren<Slider>();

        if (textLabel == null) textLabel = GetComponent<TextMeshProUGUI>();
        if (textLabel == null) textLabel = GetComponentInChildren<TextMeshProUGUI>();

        if (_provider == null)
        {
            _provider = GetComponent<IValueProvider>();
            if (_provider == null) _provider = GetComponentInChildren<IValueProvider>();
            if (_provider == null) _provider = GetComponentInParent<IValueProvider>();
        }

        // Provider를 찾았다면 곧바로 초기화 수행
        if (_provider != null)
        {
            SetProvider(_provider);
        }
    }

    void Update()
    {
        // 추적할 대상이나 데이터 제공자가 없으면 아무것도 하지 않음
        if (_provider == null) return;

        // 1. Provider를 통해 대상의 현재 값과 최대값을 실시간으로 가져옴
        float current = _provider.GetCurrentValue(_targetUnit);
        float max = _provider.GetMaxValue(_targetUnit);
        
        // 최대값이 0보다 크면 퍼센트(0.0 ~ 1.0) 계산, 아니면 0으로 고정하여 오류 방지
        float percent = (max > 0) ? current / max : 0f;

        // Debug.Log($"[Bar Log] Current: {current}, Max: {max}, SliderValue: {current / max}");

        // 2. 계산된 퍼센트와 현재 목표값(_target)이 다르면 애니메이션 작동 플래그 켜기
        if (!Mathf.Approximately(_target, percent))
        {
            SetValue(percent);
        }

        // 3. 텍스트 UI 업데이트 (예: "HP 50 / 100")
        if (textLabel != null)
        {
            textLabel.text = $"{_prefix}{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        // 4. 슬라이더 애니메이션 실제 동작 처리
        if (!_isAnimating) return;
        if (bar == null) return;

        // Time.deltaTime을 곱해 프레임에 상관없이 부드럽게 목표치까지 이동
        bar.value = Mathf.MoveTowards(bar.value, _target, Time.deltaTime * speed);

        // 슬라이더 값이 목표값에 도달하면 애니메이션 종료
        if (Mathf.Approximately(bar.value, _target))
        {
            bar.value = _target; // 오차 보정 (정확히 목표값으로 고정)
            _isAnimating = false;
        }
    }

    // 목표값을 새로 설정하고 애니메이션을 시작하는 함수
    public void SetValue(float newValue)
    {
        _target = newValue;
        _isAnimating = true;
    }

    // 바(Bar)가 어떤 데이터를 추적할지 초기 세팅해주는 함수
    public void SetProvider(IValueProvider provider)
    {
        _provider = provider;
        _targetUnit = provider.GetTarget();
        _prefix = provider.text + " "; // 예: "HP ", "MP " 등 텍스트 앞에 붙을 글자 설정

        // 초기 설정 시 슬라이더 바의 값을 즉시(애니메이션 없이) 세팅
        if (_provider != null)
        {
            float current = _provider.GetCurrentValue(_targetUnit);
            float max = _provider.GetMaxValue(_targetUnit);
            float percent = (max > 0) ? current / max : 0f;

            _target = percent;
            if (bar != null) bar.value = percent;
        }
    }
}