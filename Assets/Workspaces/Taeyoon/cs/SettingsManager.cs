using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Image brightnessOverlay;
    [SerializeField] private Slider brightnessSlider;

    private void Start()
    {
        // 저장된 밝기 불러오기 (기본값 1.0f)
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);

        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        SetBrightness(savedBrightness);
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
    }
}