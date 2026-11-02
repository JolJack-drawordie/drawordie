using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// 설정 창: 밝기 / 배경음 / 효과음 조절, 게임 종료, 로비로 돌아가기(저장 여부 확인), 로그아웃.
// 설정 창이 열려 있는 동안은 게임이 일시정지된다.
public class SettingsManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Brightness Controls")]
    [SerializeField] private Image brightnessOverlay; // DontDestroyCanvas가 없을 때 대신 쓰는 밝기 덮개
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessLabelText;
    [SerializeField] private TextMeshProUGUI brightnessValueText;

    [Header("BGM Controls")]
    [FormerlySerializedAs("soundSlider")]
    [SerializeField] private Slider bgmSlider;
    [FormerlySerializedAs("soundLabelText")]
    [SerializeField] private TextMeshProUGUI bgmLabelText;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private Toggle bgmToggle; // 켜짐 = 소리 켜짐

    [Header("SFX Controls")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxLabelText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Toggle sfxToggle; // 켜짐 = 소리 켜짐

    [Header("Exit / Lobby")]
    // 게임 종료 / 로비로 / 로그아웃 전에 띄우는 확인 창
    [SerializeField] private ConfirmDialog confirmDialog;
    // 로비로 버튼 (로비 씬에는 없음)
    [SerializeField] private Button lobbyButton;

    private const string LobbySceneName = "LobbyScene";
    private const string TitleSceneName = "TitleScreen";
    private bool isLeaving; // 로비 / 타이틀로 이동 중 (중복 클릭 방지)

    // 음소거를 풀 때 저장된 음량이 이보다 작으면(슬라이더를 끝까지 내려 끈 경우) 기본 음량으로 되돌린다
    private const float MinRestoreVolume = 0.05f;
    private const float DefaultRestoreVolume = 0.5f;

    private void Awake()
    {
        // 설정 창이 켜져 있는 동안 게임 일시정지 (로비는 LobbyManager가 패널을 직접 켜므로 패널에 붙임)
        if (settingsPanel != null && settingsPanel.GetComponent<PauseWhileActive>() == null)
            settingsPanel.AddComponent<PauseWhileActive>();
    }

    private void Start()
    {
        // 저장된 밝기 적용
        float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);

        if (brightnessSlider != null)
        {
            brightnessSlider.value = savedBrightness;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
        SetBrightness(savedBrightness);

        // 음량은 SoundManager가 시작할 때 불러와 두므로 UI만 맞춘다 (음소거 중이면 슬라이더 0)
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
        // 전투 중 카드 연출 / 적 턴 동안은 로비로 버튼을 막음
        if (lobbyButton != null)
            lobbyButton.interactable = !isLeaving && CanLeaveToLobby();
    }

    // 설정 창 열기
    public void OpenSettings()
    {
        if (confirmDialog != null) confirmDialog.Hide();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    // 설정 창 닫기 (설정 값 저장)
    public void CloseSettings()
    {
        PlayerPrefs.Save();
        if (confirmDialog != null) confirmDialog.Hide();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // 게임 종료 버튼: 확인 후 종료
    public void OnClickExit()
    {
        if (confirmDialog != null)
            confirmDialog.Show("정말 게임을 종료하시겠습니까?", QuitGame);
        else
            QuitGame();
    }

    // 에디터에서는 플레이 모드 종료, 빌드에서는 앱 종료
    private void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 로비로 버튼
    public void OnClickLobby()
    {
        if (isLeaving || !CanLeaveToLobby()) return;

        // 맵 / 월드맵은 저장을 지원하지 않으므로 저장 없이 이동할지만 묻는다
        if (!SaveManager.IsSaveSupportedScene())
        {
            string where = SceneManager.GetActiveScene().name == "WorldMapScene" ? "월드 맵" : "맵";
            if (confirmDialog != null)
                confirmDialog.Show($"{where}에서는 저장할 수 없습니다.\n저장하지 않고 로비로 돌아가시겠습니까?", GoToLobby);
            else
                GoToLobby();
            return;
        }

        // 예: 저장 후 이동 / 아니오: 저장하지 않고 이동 / 취소: 그대로
        if (confirmDialog != null)
            confirmDialog.ShowWithCancel("로비로 돌아가기 전에\n저장하시겠습니까?", SaveAndGoToLobby, GoToLobby);
        else
            SaveAndGoToLobby();
    }

    // 로그아웃 버튼: 확인 후 로그아웃
    public void OnClickLogout()
    {
        if (isLeaving) return;

        if (confirmDialog != null)
            confirmDialog.Show("로그아웃 하시겠습니까?", Logout);
        else
            Logout();
    }

    // 로그인 정보와 이전 계정의 런 상태를 지우고 타이틀 화면으로
    private void Logout()
    {
        isLeaving = true;
        PlayerPrefs.Save();

        // 일시정지를 먼저 풀어야 타이틀이 멈춘 채로 열리지 않음
        GamePause.Resume();

        AuthManager.Logout();
        LobbyManager.ResetRunState();

        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.IsPaused = true;

        // 로비와 타이틀은 같은 BGM이라 멈춰 두어야 타이틀에서 처음부터 다시 나온다
        if (SoundManager.Instance != null)
            SoundManager.Instance.StopBGM();

        SceneManager.LoadScene(TitleSceneName);
    }

    // 저장 버튼과 같은 조건 (전투 중이면 플레이어 턴이고 카드 연출 중이 아닐 때만)
    private static bool CanLeaveToLobby()
    {
        return SaveManager.IsSafeToSave();
    }

    // 저장한 뒤 로비로 (저장에 실패하면 저장 없이 갈지 묻는다)
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

    // 로비로 이동
    private void GoToLobby()
    {
        isLeaving = true;
        PlayerPrefs.Save();

        // 일시정지를 먼저 풀어야 로비가 멈춘 채로 열리지 않음
        GamePause.Resume();

        // 로비에 있는 동안은 플레이 시간을 재지 않음 (새 게임 / 불러오기 때 다시 시작)
        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.IsPaused = true;

        // 로비 BGM은 타이틀과 같은 메인 BGM (처음부터 재생)
        SoundManager sound = SoundManager.Instance;
        if (sound != null)
        {
            sound.StopBGM();
            if (sound.mainBackgroundSound != null) sound.PlayBGM(sound.mainBackgroundSound);
        }

        SceneManager.LoadScene(LobbySceneName);
    }

    // 밝기 적용 및 저장 (밝기 1이면 덮개 투명, 0이면 80% 검정)
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

    // 배경음 / 효과음 음량 (실제 적용과 저장은 SoundManager 담당)
    // 슬라이더를 0으로 내리면 음소거, 음소거 토글을 끄면 슬라이더도 0으로
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

    // 배경음 / 효과음 켜기 끄기 (켤 때 음량이 너무 작으면 기본 음량으로)
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

    // 모든 라벨 갱신
    private void UpdateLabels()
    {
        if (brightnessSlider != null) SetLabel(brightnessLabelText, brightnessValueText, "밝기", brightnessSlider.value);
        if (bgmSlider != null) SetLabel(bgmLabelText, bgmValueText, "배경음", bgmSlider.value);
        if (sfxSlider != null) SetLabel(sfxLabelText, sfxValueText, "효과음", sfxSlider.value);
    }

    // 왼쪽 라벨엔 이름, 오른쪽 라벨엔 % (% 라벨이 없으면 "이름: %"를 한 라벨에)
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
