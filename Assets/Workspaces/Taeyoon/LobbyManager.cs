using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button lordGameButton;
    [SerializeField] private Button settingsButton;
    // Tools > Ranking > 로비 랭킹 버튼 배치 로 씬에 배치. 비워두면 실행 시 LoadGame 버튼 아래에 자동 생성
    [SerializeField] private Button rankingButton;

    [Header("Cutscene")]
    // 새 게임을 누르면 월드맵으로 가기 전에 재생 (비워두면 바로 월드맵)
    [SerializeField] private VideoClip introVideo;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Image fadeImage;

    [Header("Message")]
    // 비워두면 실행 시 화면 하단에 자동 생성
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;

    [Header("테스트 (에디터에서만 적용)")]
    [Tooltip("새 게임을 이 Act부터 시작 (0이면 사용 안 함)")]
    [SerializeField, Range(0, 3)] private int debugStartAct = 0;
    [Tooltip("맵 시작 시 보스 노드를 바로 선택 가능")]
    [SerializeField] private bool debugStartAtBoss = false;
    [Tooltip("플레이어가 적에게 주는 피해를 최소 150으로 (테스트 때 전투를 빨리 넘기기용)")]
    [SerializeField] private bool debugBigDamage = false;

    private bool isTransitioning;
    private Coroutine messageRoutine;
    private GameObject messageCanvas;

    void Awake()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (fadeImage     != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    void Start()
    {
        // 로비 BGM은 타이틀의 메인 BGM. 엔딩/패배 후 BGM이 멈춘 채 들어와도 다시 재생 (이미 재생 중이면 그대로 이어짐)
        if (SoundManager.Instance != null && SoundManager.Instance.mainBackgroundSound != null)
            SoundManager.Instance.PlayBGM(SoundManager.Instance.mainBackgroundSound);

        if (newGameButton == null)
            Debug.LogError("[LobbyManager] newGameButton 미연결!");
        else
        {
            newGameButton.interactable = true;
            newGameButton.onClick.AddListener(OnNewGameClick);
            Debug.Log("[LobbyManager] NewGame 버튼 이벤트 등록 완료");
        }

        if (lordGameButton == null)
            Debug.LogError("[LobbyManager] lordGameButton 미연결!");
        else
        {
            lordGameButton.interactable = true;
            lordGameButton.onClick.AddListener(OnLordGameClick);
            Debug.Log("[LobbyManager] LordGame 버튼 이벤트 등록 완료");
        }

        SetupRankingButton();

        if (settingsButton == null)
            Debug.LogError("[LobbyManager] settingsButton 미연결!");
        else
        {
            settingsButton.interactable = true;
            settingsButton.onClick.AddListener(OnSettingsClick);
            Debug.Log("[LobbyManager] Settings 버튼 이벤트 등록 완료");
        }
    }

    public void OnNewGameClick()
    {
        Debug.Log("[LobbyManager] NewGame 클릭됨");
        if (isTransitioning) return;

        HideMessage();

        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.StartNewRun();

        ResetRunState();

        ApplyDebugStart();

        // 새 게임은 인트로 컷씬 후 월드맵(Act 진행도)에서 시작 → Act 1 섬을 눌러 맵으로 이동
        StartCoroutine(FadeAndLoad("WorldMapScene", introVideo));
    }

    // 설정 창의 "로비로"나 클리어 후 로비로 돌아온 경우 이전 런의 진행도 / 체력 / 덱이 남아 있으므로 초기화
    private void ResetRunState()
    {
        GameFlowData.ResetRun();

        PendingLoadData.Clear();
        PendingLoadData.pendingDeckJson = null;

        // 유물과 유물로 늘어난 최대 체력도 초기화 (ResetStat은 현재 체력을 최대 체력으로 채우므로 먼저 되돌림)
        RelicManager.ClearOwnedRelics();

        if (StatManager.Instance != null && StatManager.Instance.runtimePlayerStat != null)
        {
            StatManager.Instance.ResetPlayerMaxHp();
            StatManager.Instance.runtimePlayerStat.ResetStat();
        }

        if (DeckManager.Instance != null)
            DeckManager.Instance.ResetForNewRun();
    }

    // 테스트 설정 적용 (빌드에서는 항상 꺼짐)
    private void ApplyDebugStart()
    {
        GameFlowData.debugUnlockBoss = false;
        GameFlowData.debugBigDamage = false;

#if UNITY_EDITOR
        if (debugStartAct > 0)
        {
            GameFlowData.StartAtAct(debugStartAct);
            Debug.LogWarning($"[LobbyManager] 테스트: Act {GameFlowData.currentAct}부터 시작");
        }

        if (debugStartAtBoss)
        {
            GameFlowData.debugUnlockBoss = true;
            Debug.LogWarning("[LobbyManager] 테스트: 보스 노드 바로 선택 가능");
        }

        if (debugBigDamage)
        {
            GameFlowData.debugBigDamage = true;
            Debug.LogWarning($"[LobbyManager] 테스트: 플레이어 피해 최소 {GameFlowData.DebugBigDamageAmount}");
        }
#endif
    }

    public void OnLordGameClick()
    {
        Debug.Log("[LobbyManager] LordGame 클릭됨");

        // ⭐ [로드 기능] 추가
        if (LoadManager.Instance != null)
        {
            LoadManager.Instance.LoadGame(ShowMessage);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] LoadManager를 찾을 수 없습니다.");
        }
        // ⭐ [로드 기능] 끝
    }

    public void OnRankingClick()
    {
        Debug.Log("[LobbyManager] Ranking 클릭됨");
        RankingPanel.Toggle();
    }

    private void SetupRankingButton()
    {
        if (rankingButton != null)
        {
            rankingButton.onClick.AddListener(OnRankingClick);
            return;
        }

        if (lordGameButton == null) return;

        // LoadGame 버튼과 같은 크기로, 버튼 간격(150)만큼 아래에 배치
        RectTransform loadRect = (RectTransform)lordGameButton.transform;
        rankingButton = RankingUIFactory.CreateOpenButton(
            loadRect.parent,
            loadRect.anchoredPosition + new Vector2(0f, -150f),
            loadRect.sizeDelta,
            40f);

        // 페이드 이미지보다 아래에 그려지도록 LoadGame 버튼 바로 뒤에 배치
        rankingButton.transform.SetSiblingIndex(loadRect.GetSiblingIndex() + 1);

        Debug.Log("[LobbyManager] rankingButton 미연결 → 실행 중 자동 생성");
    }

    public void OnSettingsClick()
    {
        Debug.Log("[LobbyManager] Settings 클릭됨");
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    // 로비 화면에 잠시 메시지 표시 (예: "저장된 데이터가 없습니다.")
    public void ShowMessage(string message)
    {
        if (messageText == null)
            CreateMessageText();
        if (messageText == null) return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        messageText.text = message;
        messageText.alpha = 1f;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        // 0.5초 동안 서서히 사라짐
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            messageText.alpha = 1f - timer / 0.5f;
            yield return null;
        }

        messageText.gameObject.SetActive(false);
        messageRoutine = null;
    }

    // 로비 씬에 속한 전용 오버레이 캔버스에 만든다.
    // (FindFirstObjectByType<Canvas>로 찾으면 DontDestroyCanvas 같은 씬 전환 후에도 남는 캔버스가 잡혀,
    //  메시지가 떠 있는 중에 새 게임으로 넘어가면 글자가 다음 씬까지 계속 남는 문제가 있었음)
    private void CreateMessageText()
    {
        messageCanvas = new GameObject("LobbyMessageCanvas");

        Canvas canvas = messageCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = messageCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject obj = new GameObject("MessageText");
        obj.transform.SetParent(messageCanvas.transform, false);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        // TMP 기본 폰트에는 한글이 없어 글자가 깨지므로 한글 폰트 지정
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("온글잎 콘콘체 SDF");
        if (font != null) text.font = font;
        text.fontSize = 40f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        text.raycastTarget = false;

        // 화면 하단 중앙
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 120f);
        rect.sizeDelta = new Vector2(1000f, 80f);

        obj.SetActive(false);
        messageText = text;
    }

    private void HideMessage()
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (messageCanvas != null) Destroy(messageCanvas);
    }

    private IEnumerator FadeAndLoad(string sceneName, VideoClip cutscene = null)
    {
        isTransitioning = true;

        if (fadeImage != null)
        {
            float timer = 0f;
            while (timer < 0.8f)
            {
                timer += Time.deltaTime;
                fadeImage.color = new Color(0f, 0f, 0f, timer / 0.8f);
                yield return null;
            }
        }

        yield return WorldMapManager.PlayCutscene(cutscene, true);

        SceneManager.LoadScene(sceneName);
        // 배경 음악 정지
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
        }
    }
}
