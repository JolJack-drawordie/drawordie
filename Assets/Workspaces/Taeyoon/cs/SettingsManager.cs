using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Brightness Controls")]
    [SerializeField] private Image brightnessOverlay;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessLabelText;

    [Header("Sound Controls")]
    [SerializeField] private Slider soundSlider;
    [SerializeField] private TextMeshProUGUI soundLabelText;

    private void Start()
    {
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);
        float savedVolume = PlayerPrefs.GetFloat("Volume", 1.0f);

        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
        SetBrightness(savedBrightness);

        if (soundSlider != null)
        {
            soundSlider.value = savedVolume;
            soundSlider.onValueChanged.AddListener(SetVolume);
        }
        SetVolume(savedVolume);

        UpdateLabels();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
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

    public void SetBrightness(float value)
    {
        if (brightnessOverlay != null)
        {
            Color color = brightnessOverlay.color;
            color.a = (1.0f - value) * 0.8f;
            brightnessOverlay.color = color;
        }

        PlayerPrefs.SetFloat("Brightness", value);

        if (brightnessLabelText != null)
        {
            brightnessLabelText.text = $"밝기: {Mathf.RoundToInt(value * 100)}%";
        }
    }

    // 사운드 조절 (AudioSource 직접 제어 방식)
    public void SetVolume(float value)
    {
        // SoundManager 내의 AudioSource 볼륨을 직접 수정
        if (SoundManager.Instance != null)
        {
            if (SoundManager.Instance.sfxSource != null)
            {
                SoundManager.Instance.sfxSource.volume = value;
            }

            if (SoundManager.Instance.bgmSource != null)
            {
                SoundManager.Instance.bgmSource.volume = value;
            }
        }

        // 유니티 전체 볼륨도 같이 적용
        AudioListener.volume = value;

        PlayerPrefs.SetFloat("Volume", value);

        if (soundLabelText != null)
        {
            soundLabelText.text = $"음량: {Mathf.RoundToInt(value * 100)}%";
        }
    }

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