using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

// 로비 화면: 새 게임 / 이어하기 / 랭킹 / 설정 버튼, 화면 하단 안내 메시지, 페이드 후 씬 이동.
// 테스트용 시작 옵션(시작 Act, 보스 바로 선택, 큰 피해)은 에디터에서만 적용된다.
public class LobbyManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button lordGameButton; // 이어하기 버튼
    [SerializeField] private Button settingsButton;
    // 비워두면 실행 중에 이어하기 버튼 아래에 자동으로 만든다
    [SerializeField] private Button rankingButton;

    [Header("Cutscene")]
    // 새 게임을 눌렀을 때 월드맵 전에 재생할 인트로 영상 (비워두면 바로 월드맵)
    [SerializeField] private VideoClip introVideo;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Image fadeImage; // 씬 이동 때 화면을 덮는 검은 이미지

    [Header("Message")]
    // 비워두면 처음 표시할 때 화면 하단에 자동으로 만든다
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageDuration = 2f;

    [Header("테스트 (에디터에서만 적용)")]
    [Tooltip("새 게임을 이 Act부터 시작 (0이면 사용 안 함)")]
    [SerializeField, Range(0, 3)] private int debugStartAct = 0;
    [Tooltip("맵 시작 시 보스 노드를 바로 선택 가능")]
    [SerializeField] private bool debugStartAtBoss = false;
    [Tooltip("플레이어가 적에게 주는 피해를 최소 150으로 (테스트 때 전투를 빨리 넘기기용)")]
    [SerializeField] private bool debugBigDamage = false;

    private bool isTransitioning; // 씬 이동 중 (중복 클릭 방지)
    private Coroutine messageRoutine;
    private GameObject messageCanvas; // 자동으로 만든 메시지 전용 캔버스 (로비를 떠날 때 함께 삭제)

    void Awake()
    {
        // 설정 창은 닫고, 페이드 이미지는 투명하게 시작
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (fadeImage     != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    void Start()
    {
        // 메인 BGM 재생 (엔딩 / 패배 후 멈춘 채 들어와도 다시 재생, 이미 재생 중이면 이어짐)
        if (SoundManager.Instance != null && SoundManager.Instance.mainBackgroundSound != null)
            SoundManager.Instance.PlayBGM(SoundManager.Instance.mainBackgroundSound);

        // 버튼 이벤트 연결
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

    // 새 게임: 플레이 시간 측정 시작 → 이전 런 초기화 → 테스트 옵션 적용 → 인트로 영상 후 월드맵
    public void OnNewGameClick()
    {
        Debug.Log("[LobbyManager] NewGame 클릭됨");
        if (isTransitioning) return;

        HideMessage();

        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.StartNewRun();

        ResetRunState();

        ApplyDebugStart();

        StartCoroutine(FadeAndLoad("WorldMapScene", introVideo));
    }

    // 이전 런의 진행도 / 유물 / 체력 / 덱을 모두 초기화
    // (클리어 후 로비로 돌아온 경우, 로그아웃할 때 SettingsManager도 호출)
    public static void ResetRunState()
    {
        GameFlowData.ResetRun();

        PendingLoadData.Clear();
        PendingLoadData.pendingDeckJson = null;

        // 유물로 늘어난 최대 체력을 먼저 되돌린 뒤 체력을 채운다 (ResetStat이 현재 체력을 최대 체력으로 채우므로)
        RelicManager.ClearOwnedRelics();

        if (StatManager.Instance != null && StatManager.Instance.runtimePlayerStat != null)
        {
            StatManager.Instance.ResetPlayerMaxHp();
            StatManager.Instance.runtimePlayerStat.ResetStat();
        }

        if (DeckManager.Instance != null)
            DeckManager.Instance.ResetForNewRun();
    }

    // 테스트 옵션 적용 (에디터에서만, 빌드에서는 항상 꺼짐)
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

    // 이어하기: 서버에 저장된 게임 불러오기 (실패하면 메시지 표시)
    public void OnLordGameClick()
    {
        Debug.Log("[LobbyManager] LordGame 클릭됨");

        if (LoadManager.Instance != null)
        {
            LoadManager.Instance.LoadGame(ShowMessage);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] LoadManager를 찾을 수 없습니다.");
        }
    }

    // 랭킹 패널 열기 / 닫기
    public void OnRankingClick()
    {
        Debug.Log("[LobbyManager] Ranking 클릭됨");
        RankingPanel.Toggle();
    }

    // 랭킹 버튼이 연결돼 있으면 이벤트만 연결, 없으면 이어하기 버튼 아래에 만든다
    private void SetupRankingButton()
    {
        if (rankingButton != null)
        {
            rankingButton.onClick.AddListener(OnRankingClick);
            return;
        }

        if (lordGameButton == null) return;

        // 이어하기 버튼과 같은 크기로, 버튼 간격(150)만큼 아래에
        RectTransform loadRect = (RectTransform)lordGameButton.transform;
        rankingButton = RankingUIFactory.CreateOpenButton(
            loadRect.parent,
            loadRect.anchoredPosition + new Vector2(0f, -150f),
            loadRect.sizeDelta,
            40f);

        // 페이드 이미지보다 아래에 그려지도록 이어하기 버튼 바로 뒤 순서로
        rankingButton.transform.SetSiblingIndex(loadRect.GetSiblingIndex() + 1);

        Debug.Log("[LobbyManager] rankingButton 미연결 → 실행 중 자동 생성");
    }

    // 설정 창 열기 / 닫기
    public void OnSettingsClick()
    {
        Debug.Log("[LobbyManager] Settings 클릭됨");
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    // 화면 하단에 잠깐 메시지 표시 (예: "저장된 데이터가 없습니다.")
    public void ShowMessage(string message)
    {
        if (messageText == null)
            CreateMessageText();
        if (messageText == null) return;

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowMessageRoutine(message));
    }

    // 일정 시간 보여 준 뒤 0.5초 동안 서서히 사라짐
    private IEnumerator ShowMessageRoutine(string message)
    {
        messageText.text = message;
        messageText.alpha = 1f;
        messageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        // 서서히 사라짐
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

    // 메시지 글자를 로비 전용 오버레이 캔버스에 만든다.
    // (씬이 바뀌어도 남는 DontDestroyCanvas 같은 캔버스에 붙이면 메시지가 다음 씬까지 남기 때문)
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
        // TMP 기본 폰트는 한글이 깨지므로 한글 폰트 사용
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

    // 떠 있는 메시지 숨기기
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

    // 자동으로 만든 메시지 캔버스 정리
    private void OnDestroy()
    {
        if (messageCanvas != null) Destroy(messageCanvas);
    }

    // 화면을 검게 덮고 → 영상이 있으면 재생 → 씬 이동 후 BGM 정지
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

        // 컷씬 재생 (영상이 없으면 바로 넘어감)
        yield return WorldMapManager.PlayCutscene(cutscene, true);

        SceneManager.LoadScene(sceneName);
        // BGM 정지
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
        }
    }
}
