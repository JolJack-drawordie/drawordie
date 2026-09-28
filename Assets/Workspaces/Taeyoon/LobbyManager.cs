using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button lordGameButton;
    [SerializeField] private Button settingsButton;
    // Tools > Ranking > 로비 랭킹 버튼 배치 로 씬에 배치. 비워두면 실행 시 LoadGame 버튼 아래에 자동 생성
    [SerializeField] private Button rankingButton;

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

    private bool isTransitioning;
    private Coroutine messageRoutine;

    void Awake()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (fadeImage     != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    void Start()
    {
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

        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.StartNewRun();

        ApplyDebugStart();

        StartCoroutine(FadeAndLoad("MapScene"));
    }

    // 테스트 설정 적용 (빌드에서는 항상 꺼짐)
    private void ApplyDebugStart()
    {
        GameFlowData.debugUnlockBoss = false;

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

    private void CreateMessageText()
    {
        Canvas canvas = fadeImage != null ? fadeImage.canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[LobbyManager] 메시지를 표시할 Canvas를 찾을 수 없습니다.");
            return;
        }

        GameObject obj = new GameObject("MessageText");
        obj.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
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

        // 페이드 이미지보다 아래에 그려지도록 페이드 이미지 바로 앞에 배치
        if (fadeImage != null && fadeImage.transform.parent == canvas.transform)
            obj.transform.SetSiblingIndex(fadeImage.transform.GetSiblingIndex());

        obj.SetActive(false);
        messageText = text;
    }

    private IEnumerator FadeAndLoad(string sceneName)
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

        SceneManager.LoadScene(sceneName);
        // 배경 음악 정지
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
        }
    }
}
