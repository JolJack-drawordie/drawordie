using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용할 경우 유지, 일반 Text를 사용할 경우 아래 Text 컴포넌트로 교체해 주세요.

public class SettingsManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Brightness Controls")]
    [SerializeField] private Image brightnessOverlay;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessLabelText; // '밝기' 레이블 또는 수치 표시용

    [Header("Sound Controls")]
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TextMeshProUGUI soundLabelText;      // '음량' 레이블 또는 수치 표시용

    private void Start()
    {
        // 1. 저장된 설정값 불러오기 (기본값 1.0f)
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);
        float savedVolume = PlayerPrefs.GetFloat("Volume", 1.0f);

        // 2. 밝기 슬라이더 초기화 및 이벤트 연결
        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
        SetBrightness(savedBrightness);

        // 3. 사운드 슬라이더 초기화 및 이벤트 연결
        if (soundSlider != null)
        {
            soundSlider.value = savedVolume;
            soundSlider.onValueChanged.AddListener(SetVolume);
        }
        SetVolume(savedVolume);

        // 4. 레이블 고정 텍스트 설정 (선택 사항)
        UpdateLabels();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 밝기 조절
    public void SetBrightness(float value)
    {
        if (brightnessOverlay != null)
        {
            Color color = brightnessOverlay.color;
            // 슬라이더 값(0~1)에 따라 투명도 조절
            color.a = (1.0f - value) * 0.8f;
            brightnessOverlay.color = color;
        }

        PlayerPrefs.SetFloat("Brightness", value);
        PlayerPrefs.Save();

        // 밝기 텍스트 갱신 (예: 밝기 100%)
        if (brightnessLabelText != null)
        {
            brightnessLabelText.text = $"밝기: {Mathf.RoundToInt(value * 100)}%";
        }
    }

    // 사운드 조절 (전체 마스터 볼륨 설정)
    public void SetVolume(float value)
    {
        AudioListener.volume = value; // 유니티 전체 마스터 볼륨 적용

        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();

        // 음량 텍스트 갱신 (예: 음량 100%)
        if (soundLabelText != null)
        {
            soundLabelText.text = $"음량: {Mathf.RoundToInt(value * 100)}%";
        }
    }

    // 기본 레이블 텍스트 갱신
    private void UpdateLabels()
    {
        if (brightnessLabelText != null && brightnessSlider != null)
        {
            brightnessLabelText.text = $"밝기: {Mathf.RoundToInt(brightnessSlider.value * 100)}%";
        }

        if (soundLabelText != null && soundSlider != null)
        {
            soundLabelText.text = $"음량: {Mathf.RoundToInt(soundSlider.value * 100)}%";
        }
    }
}