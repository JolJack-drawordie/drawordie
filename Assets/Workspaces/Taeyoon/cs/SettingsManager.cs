using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Brightness Controls")]
    [SerializeField] private Image brightnessOverlay;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessLabelText;
    [SerializeField] private TextMeshProUGUI brightnessValueText;

    [Header("BGM Controls")]
    [FormerlySerializedAs("soundSlider")]
    [SerializeField] private Slider bgmSlider;
    [FormerlySerializedAs("soundLabelText")]
    [SerializeField] private TextMeshProUGUI bgmLabelText;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private Toggle bgmToggle; // 켜짐(isOn) = 소리 켜짐

    [Header("SFX Controls")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxLabelText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Toggle sfxToggle; // 켜짐(isOn) = 소리 켜짐

    [Header("Exit / Lobby")]
    // 게임 종료 / 로비로 전에 띄우는 확인 창 (Tools > Settings > 설정 창 UI 배치 로 생성)
    [SerializeField] private ConfirmDialog confirmDialog;
    // 로비 씬에는 없음
    [SerializeField] private Button lobbyButton;

    private const string LobbySceneName = "LobbyScene";
    private bool isLeaving;

    // 음소거를 풀 때 저장된 음량이 이보다 작으면(슬라이더를 끝까지 내려 끈 경우) 기본 음량으로 되돌린다
    private const float MinRestoreVolume = 0.05f;
    private const float DefaultRestoreVolume = 0.5f;

    private void Awake()
    {
        // 설정 창이 열려 있는 동안 게임 일시정지 (로비는 LobbyManager가 패널을 직접 켜므로 패널 자체에 붙임)
        if (settingsPanel != null && settingsPanel.GetComponent<PauseWhileActive>() == null)
            settingsPanel.AddComponent<PauseWhileActive>();
    }

    private void Start()
    {
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);

        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
        SetBrightness(savedBrightness);

        // 저장된 음량은 SoundManager가 시작할 때 불러와 두므로 UI만 맞춰준다 (음소거 중이면 슬라이더는 0)
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            float bgm = sound.BGMEnabled ? sound.BGMVolume : 0f;
            float sfx = sound.SFXEnabled ? sound.SFXVolume : 0f;
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(bgm);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);
            if (bgmToggle != null) bgmToggle.SetIsOnWithoutNotify(bgm > 0f);
            if (sfxToggle != null) sfxToggle.SetIsOnWithoutNotify(sfx > 0f);
        }

        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (bgmToggle != null) bgmToggle.onValueChanged.AddListener(SetBGMEnabled);
        if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(SetSFXEnabled);

        UpdateLabels();
    }

    private void Update()
    {
        // 전투 중에는 카드 연출 / 적 턴 동안 로비로 버튼을 회색 처리
        if (lobbyButton != null)
            lobbyButton.interactable = !isLeaving && CanLeaveToLobby();
    }

    public void OpenSettings()
    {
        if (confirmDialog != null) confirmDialog.Hide();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        if (confirmDialog != null) confirmDialog.Hide();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnClickExit()
    {
        if (confirmDialog != null)
            confirmDialog.Show("정말 게임을 종료하시겠습니까?", QuitGame);
        else
            QuitGame();
    }

    private void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickLobby()
    {
        if (isLeaving || !CanLeaveToLobby()) return;

        // 예: 저장 후 이동 / 아니오: 저장하지 않고 이동 / 취소: 그대로 머무름
        if (confirmDialog != null)
            confirmDialog.ShowWithCancel("로비로 돌아가기 전에\n저장하시겠습니까?", SaveAndGoToLobby, GoToLobby);
        else
            SaveAndGoToLobby();
    }

    // 저장 버튼과 같은 조건 (전투 중이면 플레이어 턴 + 카드 연출 중이 아닐 때만)
    private static bool CanLeaveToLobby()
    {
        return SaveManager.IsSafeToSave();
    }

    private void SaveAndGoToLobby()
    {
        isLeaving = true;

        // 로그인하지 않았거나 이미 끝난 런이면 저장 없이 이동
        bool canSave = SaveManager.Instance != null && AuthManager.isLoggedIn &&
                       (PlayTimeTracker.Instance == null || !PlayTimeTracker.Instance.IsRunEnded);
        if (!canSave)
        {
            GoToLobby();
            return;
        }

        // 저장이 끝난 뒤에 씬을 바꿔야 저장 요청이 끊기지 않음
        SaveManager.Instance.SaveGame(success =>
        {
            if (success)
            {
                GoToLobby();
                return;
            }

            isLeaving = false;
            if (confirmDialog != null)
                confirmDialog.Show("저장에 실패했습니다.\n저장하지 않고 로비로 돌아가시겠습니까?", GoToLobby);
        });
    }

    private void GoToLobby()
    {
        isLeaving = true;
        PlayerPrefs.Save();

        // 설정 창 일시정지를 먼저 풀어야 로비가 멈춘 채로 열리지 않음
        GamePause.Resume();

        // 로비에 있는 동안은 플레이 타임을 재지 않음 (새 게임 / 불러오기 시 다시 시작)
        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.IsPaused = true;

        // 로비 BGM은 타이틀에서 틀던 메인 BGM
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            sound.StopBGM();
            if (sound.mainBackgroundSound != null) sound.PlayBGM(sound.mainBackgroundSound);
        }

        SceneManager.LoadScene(LobbySceneName);
    }

    public void SetBrightness(float value)
    {
        Image overlay = DontDestroyCanvas.Instance != null
            ? DontDestroyCanvas.Instance.Overlay
            : brightnessOverlay;

        if (overlay != null)
        {
            Color color = overlay.color;
            color.a = (1.0f - value) * 0.8f;
            overlay.color = color;
        }

        PlayerPrefs.SetFloat("Brightness", value);

        SetLabel(brightnessLabelText, brightnessValueText, "밝기", value);
    }

    // 배경음 / 효과음 음량 조절 (실제 적용과 저장은 SoundManager가 담당)
    // 슬라이더를 0으로 내리면 음소거, 음소거 아이콘을 누르면 슬라이더도 0으로 내려간다
    public void SetBGMVolume(float value)
    {
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            if (value > 0f) sound.SetBGMVolume(value); // 0일 때는 음소거만 하고 이전 음량은 기억
            sound.SetBGMEnabled(value > 0f);
        }

        if (bgmToggle != null) bgmToggle.SetIsOnWithoutNotify(value > 0f);
        UpdateLabels();
    }

    public void SetSFXVolume(float value)
    {
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            if (value > 0f) sound.SetSFXVolume(value);
            sound.SetSFXEnabled(value > 0f);
        }

        if (sfxToggle != null) sfxToggle.SetIsOnWithoutNotify(value > 0f);
        UpdateLabels();
    }

    public void SetBGMEnabled(bool enabled)
    {
        float restore = DefaultRestoreVolume;
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            if (enabled && sound.BGMVolume < MinRestoreVolume) sound.SetBGMVolume(DefaultRestoreVolume);
            sound.SetBGMEnabled(enabled);
            restore = sound.BGMVolume;
        }

        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(enabled ? restore : 0f);
        UpdateLabels();
    }

    public void SetSFXEnabled(bool enabled)
    {
        float restore = DefaultRestoreVolume;
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            if (enabled && sound.SFXVolume < MinRestoreVolume) sound.SetSFXVolume(DefaultRestoreVolume);
            sound.SetSFXEnabled(enabled);
            restore = sound.SFXVolume;
        }

        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(enabled ? restore : 0f);
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if (brightnessSlider != null) SetLabel(brightnessLabelText, brightnessValueText, "밝기", brightnessSlider.value);
        if (bgmSlider != null) SetLabel(bgmLabelText, bgmValueText, "배경음", bgmSlider.value);
        if (sfxSlider != null) SetLabel(sfxLabelText, sfxValueText, "효과음", sfxSlider.value);
    }

    // 왼쪽 라벨엔 이름, 오른쪽 라벨엔 %를 표시 (% 라벨이 없으면 "이름: %"를 한 라벨에)
    private static void SetLabel(TextMeshProUGUI nameText, TextMeshProUGUI valueText, string label, float value)
    {
        string percent = $"{Mathf.RoundToInt(value * 100)}%";

        if (valueText != null)
        {
            valueText.text = percent;
            if (nameText != null) nameText.text = label;
        }
        else if (nameText != null)
        {
            nameText.text = $"{label}: {percent}";
        }
    }
}
