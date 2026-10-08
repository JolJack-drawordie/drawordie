using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 휴식 / 전투 씬의 저장 버튼.
// 전투 중 카드 연출 / 적 턴 / 저장 중에는 회색 처리되고 (설정 창의 로비로 버튼과 같은 조건),
// 저장이 끝나면 화면에 빨간 글씨로 결과를 잠깐 보여준다.
public class SaveButton : MonoBehaviour
{
    public Button button;

    private const string SuccessMessage = "저장되었습니다.";
    private const string FailMessage = "저장에 실패했습니다.";

    // 메시지 글꼴 (Assets/Workspaces/Junha/Resources 안의 폰트 에셋 이름)
    private const string MessageFontName = "온글잎 콘콘체 SDF";
    private static readonly Color MessageColor = new Color(0.85f, 0.12f, 0.1f);
    private const float MessageFontSize = 60f;
    private const float MessageY = 200f; // 화면 가운데에서 위로
    private const float MessageShowSeconds = 1.2f;
    private const float MessageFadeSeconds = 0.5f;

    private GameObject messageCanvas;
    private TextMeshProUGUI messageText;
    private Coroutine messageRoutine;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClickSave);
        }
    }

    private void Update()
    {
        if (button != null)
            button.interactable = CanSave();
    }

    private static bool CanSave()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.IsSaving) return false;
        return SaveManager.IsSafeToSave();
    }

    private void OnClickSave()
    {
        if (!CanSave()) return;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame(success => ShowMessage(success ? SuccessMessage : FailMessage));
        }
        else
        {
            Debug.LogWarning("[SaveButton] SaveManager를 찾을 수 없습니다.");
        }
    }

    private void ShowMessage(string message)
    {
        // 응답이 오기 전에 씬이 바뀌어 이 버튼이 사라졌으면 표시하지 않음
        if (this == null || !isActiveAndEnabled) return;

        if (messageText == null) CreateMessageUI();

        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(ShowMessageRoutine(message));
    }

    // 잠깐 보였다가 서서히 사라짐 (일시정지 중에도 동작하도록 실제 시간 기준)
    private IEnumerator ShowMessageRoutine(string message)
    {
        messageText.text = message;
        messageText.alpha = 1f;
        messageCanvas.SetActive(true);

        yield return new WaitForSecondsRealtime(MessageShowSeconds);

        float timer = 0f;
        while (timer < MessageFadeSeconds)
        {
            timer += Time.unscaledDeltaTime;
            messageText.alpha = 1f - timer / MessageFadeSeconds;
            yield return null;
        }

        messageCanvas.SetActive(false);
        messageRoutine = null;
    }

    // 다른 UI 위에 보이도록 별도의 오버레이 캔버스에 메시지를 만든다 (PlayTimeTracker 타이머와 같은 방식)
    private void CreateMessageUI()
    {
        messageCanvas = new GameObject("SaveMessageCanvas");

        Canvas canvas = messageCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001; // 플레이 타임(1000)보다도 위

        CanvasScaler scaler = messageCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObj = new GameObject("SaveMessageText");
        textObj.transform.SetParent(messageCanvas.transform, false);

        messageText = textObj.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(MessageFontName);
        if (font != null)
            messageText.font = font;
        else
            Debug.LogWarning($"[SaveButton] Resources에서 폰트를 찾을 수 없습니다: {MessageFontName}");

        messageText.fontSize = MessageFontSize;
        messageText.color = MessageColor;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.outlineWidth = 0.2f;
        messageText.outlineColor = Color.black;
        messageText.raycastTarget = false; // 클릭을 막지 않도록

        RectTransform rect = messageText.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, MessageY);
        rect.sizeDelta = new Vector2(1000f, 100f);

        messageCanvas.SetActive(false);
    }

    private void OnDestroy()
    {
        if (messageCanvas != null) Destroy(messageCanvas);
    }
}
