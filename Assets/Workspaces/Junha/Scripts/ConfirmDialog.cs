using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "정말 ~하시겠습니까?" 확인 창. 예 / 아니오를 누르면 각각 넘겨받은 동작을 실행한다 (아니오 동작이 없으면 그냥 닫음).
// 취소 버튼은 ShowWithCancel로 열 때만 보이며, 아무것도 하지 않고 닫는다.
// 설정 창 안에 Tools > Settings > 설정 창 UI 배치 로 만들어지며 평소에는 꺼져 있다.
public class ConfirmDialog : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Button cancelButton;
    // 보이는 버튼들을 가운데 기준으로 나란히 놓을 때 버튼 중심 사이 간격
    [SerializeField] private float buttonSpacing = 240f;

    private Action onConfirm;
    private Action onDeny;
    private bool listenersAdded;

    // [예] [아니오]   deny: 아니오를 눌렀을 때 실행할 동작 (없으면 그냥 닫기만 함)
    public void Show(string message, Action confirm, Action deny = null)
    {
        Open(message, confirm, deny, false);
    }

    // [예] [아니오] [취소]
    public void ShowWithCancel(string message, Action confirm, Action deny)
    {
        Open(message, confirm, deny, true);
    }

    public void Hide()
    {
        onConfirm = null;
        onDeny = null;
        gameObject.SetActive(false);
    }

    private void Open(string message, Action confirm, Action deny, bool withCancel)
    {
        AddListeners();

        if (messageText != null) messageText.text = message;
        onConfirm = confirm;
        onDeny = deny;

        if (cancelButton != null) cancelButton.gameObject.SetActive(withCancel);
        ArrangeButtons();

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    // 켜져 있는 버튼만 가운데 정렬로 배치 (2개 / 3개)
    private void ArrangeButtons()
    {
        Button[] buttons = { yesButton, noButton, cancelButton };

        int count = 0;
        foreach (Button button in buttons)
            if (button != null && button.gameObject.activeSelf) count++;

        int index = 0;
        foreach (Button button in buttons)
        {
            if (button == null || !button.gameObject.activeSelf) continue;

            RectTransform rect = (RectTransform)button.transform;
            float x = (index - (count - 1) / 2f) * buttonSpacing;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            index++;
        }
    }

    private void OnYes()
    {
        Action confirm = onConfirm;
        Hide();
        confirm?.Invoke();
    }

    private void OnNo()
    {
        Action deny = onDeny;
        Hide();
        deny?.Invoke();
    }

    // 꺼진 채로 시작하므로 Awake 대신 처음 열 때 연결
    private void AddListeners()
    {
        if (listenersAdded) return;
        listenersAdded = true;

        if (yesButton != null) yesButton.onClick.AddListener(OnYes);
        if (noButton != null) noButton.onClick.AddListener(OnNo);
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
    }
}
