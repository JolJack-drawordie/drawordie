using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 체력 / 방어도 바. Provider(HpProvider / ShieldProvider)가 주는 현재 값 / 최대 값을 읽어
// 슬라이더를 부드럽게 따라가게 하고 "HP 50 / 100" 같은 글자를 표시한다.
public class AnimatedBar : MonoBehaviour
{
    [Header("UI Components")]
    public Slider bar;
    public TextMeshProUGUI textLabel;

    [Header("애니메이션 설정")]
    public float speed = 2f; // 슬라이더가 차오르거나 깎이는 속도

    private string _prefix; // 글자 앞머리 (예: "HP ")
    private IValueProvider _provider;
    private UnitBase _targetUnit;

    private float _target = 0f; // 슬라이더가 향하는 목표 비율 (0~1)
    private bool _isAnimating = false;

    void Awake()
    {
        // 인스펙터 칸이 비어 있을 때만 스스로 찾음
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

        // Provider를 찾았으면 바로 초기화
        if (_provider != null)
        {
            SetProvider(_provider);
        }
    }

    void Update()
    {
        // 값을 줄 Provider가 없으면 아무것도 하지 않음
        if (_provider == null) return;

        // 대상의 현재 값과 최대 값을 매 프레임 읽어 비율 계산 (최대 값이 0이면 0)
        float current = _provider.GetCurrentValue(_targetUnit);
        float max = _provider.GetMaxValue(_targetUnit);

        float percent = (max > 0) ? current / max : 0f;

        // 비율이 바뀌면 그쪽으로 움직이기 시작
        if (!Mathf.Approximately(_target, percent))
        {
            SetValue(percent);
        }

        // 글자 갱신 (예: "HP 50 / 100")
        if (textLabel != null)
        {
            textLabel.text = $"{_prefix}{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        // 목표 비율까지 부드럽게 이동
        if (!_isAnimating) return;
        if (bar == null) return;

        bar.value = Mathf.MoveTowards(bar.value, _target, Time.deltaTime * speed);

        if (Mathf.Approximately(bar.value, _target))
        {
            bar.value = _target;
            _isAnimating = false;
        }
    }

    // 목표 비율을 새로 정하고 움직이기 시작
    public void SetValue(float newValue)
    {
        _target = newValue;
        _isAnimating = true;
    }

    // 이 바가 어떤 값을 보여 줄지 연결하고, 처음 값은 애니메이션 없이 바로 맞춘다
    public void SetProvider(IValueProvider provider)
    {
        _provider = provider;
        _targetUnit = provider.GetTarget();
        _prefix = provider.text + " ";

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