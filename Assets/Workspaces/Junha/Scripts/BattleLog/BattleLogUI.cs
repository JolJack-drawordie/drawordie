using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BattleLog 내용을 전투 화면 오른쪽(주사위 / 턴 종료 버튼 위)에 보여주는 패널.
// - 위쪽 바의 ▲ 버튼으로 줄이면 바만 남고, ▼ 버튼으로 펼치면 기록이 보인다 (스크롤로 이번 전투 전체 확인)
// - 위쪽 바(BattleLogDragBar)를 끌면 이동, 펼친 상태에서 가장자리(BattleLogResizeHandle)를 끌면 크기 조절 (처음 크기보다 작아지지 않음)
// - 위치 / 크기 / 펼침 상태는 PlayerPrefs에 저장해서 다음 전투에도 그대로 쓴다
// - 전투가 끝나면 결과 화면을 가리지 않도록 숨긴다
public class BattleLogUI : MonoBehaviour
{
    [Header("UI 연결")]
    public RectTransform panel; // 위쪽 기준(pivot y = 1)이라 높이를 늘리면 아래로 펼쳐짐
    public ScrollRect scrollRect;
    public TMP_Text logText;
    public Button toggleButton;
    public TMP_Text toggleLabel;
    public GameObject resizeHandles; // 펼쳤을 때만 보이는 크기 조절 영역들

    [Header("크기")]
    public float barHeight = 32f; // 줄였을 때의 높이 (바만 보임)
    public Vector2 minExpandedSize = new Vector2(330f, 220f); // 펼친 크기의 최소값 (= 처음 크기)
    public bool startExpanded = true;

    private const string PrefsKey = "BattleLog.Layout";

    // PlayerPrefs에 JSON으로 저장하는 배치 정보
    [System.Serializable]
    private class Layout
    {
        public Vector2 position;
        public Vector2 size;
        public bool expanded;
    }

    private bool expanded;
    private Vector2 expandedSize;
    private Canvas rootCanvas;
    private float lastTextWidth;
    private readonly StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);

        expandedSize = minExpandedSize;
        expanded = startExpanded;
        LoadLayout();
        SetExpanded(expanded, save: false);
    }

    private void OnEnable()
    {
        BattleLog.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        BattleLog.OnChanged -= Refresh;
    }

    private void Update()
    {
        // 결과 화면보다 위에 그려지므로 전투가 끝나면 숨긴다
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            gameObject.SetActive(false);
    }

    // 화면 픽셀 이동량을 캔버스 단위로 바꿀 때 나누는 값
    public float CanvasScale
    {
        get
        {
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            return rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        }
    }

    // ▲/▼ 버튼
    public void Toggle()
    {
        SetExpanded(!expanded, save: true);
    }

    // 펼치기 / 줄이기 적용 (save: 상태를 저장할지)
    private void SetExpanded(bool value, bool save)
    {
        expanded = value;
        if (panel != null)
            panel.sizeDelta = new Vector2(expandedSize.x, expanded ? expandedSize.y : barHeight);
        if (scrollRect != null && scrollRect.viewport != null)
            scrollRect.viewport.gameObject.SetActive(expanded);
        if (resizeHandles != null) resizeHandles.SetActive(expanded);
        if (toggleLabel != null) toggleLabel.text = expanded ? "▲" : "▼";

        ClampToScreen(); // 아래로 펼쳤을 때 화면 밖으로 나가면 위로 밀어 올림
        Refresh();
        ScrollToBottom();
        if (save) SaveLayout();
    }

    // 위쪽 바를 끌 때 호출
    public void MoveBy(Vector2 delta)
    {
        if (panel == null) return;
        panel.anchoredPosition += delta;
        ClampToScreen();
    }

    // 가장자리를 끌어 크기 조절. horizontal: -1 왼쪽 변 / +1 오른쪽 변 / 0 없음, vertical: -1 아래 변 / 0 없음
    // 반대쪽 변과 위 변(바)은 제자리에 둔다
    public void ResizeBy(Vector2 delta, int horizontal, int vertical)
    {
        if (panel == null || !expanded) return;
        RectTransform parent = panel.parent as RectTransform;
        if (parent == null) return;

        Rect area = parent.rect;
        Vector2 pos = panel.anchoredPosition;
        Vector2 size = panel.sizeDelta;

        float left = pos.x - size.x * panel.pivot.x;
        float right = left + size.x;
        float top = pos.y + size.y * (1f - panel.pivot.y);
        float bottom = top - size.y;

        if (horizontal < 0) left = Mathf.Clamp(left + delta.x, area.xMin, right - minExpandedSize.x);
        if (horizontal > 0) right = Mathf.Clamp(right + delta.x, left + minExpandedSize.x, area.xMax);
        if (vertical < 0) bottom = Mathf.Clamp(bottom + delta.y, area.yMin, top - minExpandedSize.y);

        expandedSize = new Vector2(right - left, top - bottom);
        panel.sizeDelta = expandedSize;
        panel.anchoredPosition = new Vector2(left + expandedSize.x * panel.pivot.x, bottom + expandedSize.y * panel.pivot.y);
    }

    // 패널이 화면 안에 완전히 들어오도록 위치 보정
    public void ClampToScreen()
    {
        if (panel == null) return;
        RectTransform parent = panel.parent as RectTransform;
        if (parent == null) return;

        Rect area = parent.rect;
        Vector2 size = panel.rect.size;
        Vector2 pivot = panel.pivot;
        Vector3 pos = panel.localPosition;

        pos.x = Mathf.Clamp(pos.x, area.xMin + size.x * pivot.x, area.xMax - size.x * (1f - pivot.x));
        pos.y = Mathf.Clamp(pos.y, area.yMin + size.y * pivot.y, area.yMax - size.y * (1f - pivot.y));
        panel.localPosition = pos;
    }

    // 지금 위치 / 크기 / 펼침 상태 저장
    public void SaveLayout()
    {
        if (panel == null) return;
        Layout layout = new Layout { position = panel.anchoredPosition, size = expandedSize, expanded = expanded };
        try
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(layout));
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BattleLogUI] 배치 저장 실패: {e.Message}");
        }
    }

    // 저장된 배치 불러오기 (저장된 크기가 최소 크기보다 작으면 최소 크기로)
    private void LoadLayout()
    {
        if (panel == null || !PlayerPrefs.HasKey(PrefsKey)) return;

        Layout layout = JsonUtility.FromJson<Layout>(PlayerPrefs.GetString(PrefsKey));
        if (layout == null) return;

        panel.anchoredPosition = layout.position;
        expandedSize = Vector2.Max(layout.size, minExpandedSize);
        expanded = layout.expanded;
    }

    // 인스펙터 ⋮ 메뉴에서 실행: 저장된 배치를 지워 처음 상태로
    [ContextMenu("저장된 배치 초기화")]
    private void ResetLayout()
    {
        PlayerPrefs.DeleteKey(PrefsKey);
    }

    private void LateUpdate()
    {
        // 크기 조절로 글 영역 너비가 바뀌면 턴 구분선 길이를 다시 맞춘다
        if (logText != null && expanded && !Mathf.Approximately(logText.rectTransform.rect.width, lastTextWidth))
            Refresh();
    }

    // 기록 전체를 색 / 정렬 태그를 붙여 한 문자열로 다시 만든다
    private void Refresh()
    {
        if (logText == null) return;

        // 펼쳐서 위쪽 기록을 보고 있을 때는 새 줄이 추가돼도 스크롤을 끌어내리지 않음
        bool stickToBottom = !expanded || scrollRect == null || scrollRect.verticalNormalizedPosition <= 0.01f;

        lastTextWidth = logText.rectTransform.rect.width;
        float dashWidth = -1f;

        sb.Clear();
        for (int i = 0; i < BattleLog.Entries.Count; i++)
        {
            BattleLog.Entry entry = BattleLog.Entries[i];
            if (i > 0) sb.Append('\n');
            if (entry.Center) sb.Append("<align=center>");
            sb.Append("<color=").Append(BattleLog.ColorOf(entry.Kind)).Append('>');

            if (entry.Turn > 0)
            {
                if (dashWidth < 0f) dashWidth = Mathf.Max(1f, logText.GetPreferredValues("──────────").x / 10f); // 선 문자 한 개의 너비 (한 번만 계산)
                AppendDivider(entry.Message, dashWidth);
            }
            else
            {
                sb.Append(entry.Message);
            }

            sb.Append("</color>");
            if (entry.Center) sb.Append("</align>");
        }
        logText.text = sb.ToString();

        if (stickToBottom) ScrollToBottom();
    }

    // "──── 3턴 ────": 글 영역 끝에서 끝까지 선을 채운다
    private void AppendDivider(string label, float dashWidth)
    {
        string middle = $" {label} ";
        float labelWidth = logText.GetPreferredValues(middle).x;
        // 줄바꿈되지 않도록 한 칸 여유를 둠
        int perSide = Mathf.Max(2, Mathf.FloorToInt((lastTextWidth - labelWidth) / 2f / dashWidth) - 1);

        sb.Append('─', perSide).Append(middle).Append('─', perSide);
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null) return;
        // 글 높이(ContentSizeFitter)가 먼저 갱신돼야 맨 아래 위치가 맞는다
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
