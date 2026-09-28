using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 한 판(런)의 총 플레이 타임을 측정한다.
// 새 게임 시작 시 0부터, 불러오기 시 저장된 시간부터 누적하며
// 씬이 바뀌어도 유지되고, 앱이 백그라운드로 가거나 일시정지 중일 때는 멈춘다.
// 게임 플레이 씬에서는 전투 씬 저장 버튼의 왼쪽 위치에 플레이 타임을 표시한다.
public class PlayTimeTracker : MonoBehaviour
{
    public static PlayTimeTracker Instance;

    // 플레이 타임을 표시하지 않는 씬 (타이틀 / 로그인 / 로비)
    private static readonly string[] HiddenScenes = { "TitleScreen", "AuthScene", "LobbyScene" };

    // 누적 플레이 타임(초)
    public float ElapsedSeconds { get; private set; }

    // 현재 런이 진행 중인지 (최종 보스 처치 / 사망 시 false)
    public bool IsRunning { get; private set; }

    // 일시정지 메뉴 등에서 설정
    public bool IsPaused { get; set; }

    private bool hasFocus = true;

    // 새 게임 / 불러오기로 런이 시작된 적이 있는지 (종료 후에도 결과 화면에서 최종 시간 표시)
    private bool hasRun;

    // 타이머 오른쪽 끝 위치 (1920x1080 기준, 화면 중앙 기준 좌표)
    // 전투 씬 저장 버튼: 중심 (814, 477), 너비 약 202.6 → 왼쪽 끝 x ≈ 712.7, 거기서 20 간격
    private static readonly Vector2 TimerPosition = new Vector2(693f, 477f);

    private GameObject timerCanvas;
    private TextMeshProUGUI timerText;
    private int lastShownSeconds = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        if (Instance != null) return;

        GameObject obj = new GameObject("PlayTimeTracker");
        obj.AddComponent<PlayTimeTracker>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateTimerUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (IsRunning && !IsPaused && hasFocus)
            ElapsedSeconds += Time.unscaledDeltaTime;

        RefreshTimerText();
    }

    private void OnApplicationFocus(bool focus)
    {
        hasFocus = focus;
    }

    private void OnApplicationPause(bool pause)
    {
        hasFocus = !pause;
    }

    // 새 게임 시작
    public void StartNewRun()
    {
        ElapsedSeconds = 0f;
        IsPaused = false;
        IsRunning = true;
        hasRun = true;
    }

    // 세이브 불러오기 시 저장된 시간부터 이어서 측정
    public void ResumeRun(int savedSeconds)
    {
        ElapsedSeconds = Mathf.Max(0, savedSeconds);
        IsPaused = false;
        IsRunning = true;
        hasRun = true;
    }

    // 최종 보스 처치 또는 사망 시 측정 종료
    public void StopRun()
    {
        if (!IsRunning) return;

        IsRunning = false;
        Debug.Log($"[PlayTimeTracker] 런 종료. 총 플레이 타임: {GetElapsedSecondsInt()}초");
    }

    public int GetElapsedSecondsInt()
    {
        return Mathf.FloorToInt(ElapsedSeconds);
    }

    // 초 → "mm:ss" (1시간 이상이면 "h:mm:ss")
    public static string FormatTime(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = totalSeconds % 3600 / 60;
        int seconds = totalSeconds % 60;

        return hours > 0
            ? $"{hours}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }

    // =========================
    // 화면 표시
    // =========================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isHiddenScene = System.Array.IndexOf(HiddenScenes, scene.name) >= 0;
        timerCanvas.SetActive(hasRun && !isHiddenScene);
    }

    private void CreateTimerUI()
    {
        timerCanvas = new GameObject("PlayTimeCanvas");
        timerCanvas.transform.SetParent(transform, false);

        Canvas canvas = timerCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 다른 UI 위에 표시

        CanvasScaler scaler = timerCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObj = new GameObject("PlayTimeText");
        textObj.transform.SetParent(timerCanvas.transform, false);

        timerText = textObj.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 36f;
        timerText.alignment = TextAlignmentOptions.Right;
        timerText.color = Color.white;
        timerText.outlineWidth = 0.2f;
        timerText.outlineColor = Color.black;
        timerText.raycastTarget = false; // 클릭을 막지 않도록

        // 전투 씬 저장 버튼 왼쪽 (오른쪽 끝을 기준으로 배치)
        RectTransform rect = timerText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = TimerPosition;
        rect.sizeDelta = new Vector2(300f, 60f);

        timerCanvas.SetActive(false);
    }

    private void RefreshTimerText()
    {
        if (!timerCanvas.activeSelf) return;

        int seconds = GetElapsedSecondsInt();
        if (seconds == lastShownSeconds) return;

        lastShownSeconds = seconds;
        timerText.text = FormatTime(seconds);
    }
}
