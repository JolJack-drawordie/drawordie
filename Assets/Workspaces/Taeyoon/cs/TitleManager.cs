using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TitleManager : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup logoGroup;
    public CanvasGroup btnStartGroup;
    public CanvasGroup btnSettingsGroup;

    private Canvas _canvas;
    private RectTransform _btnStartRect;
    private bool _isLoading = false;

    void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "AuthScene")
            _isLoading = false;
    }

    void Start()
    {
        DOTween.SetTweensCapacity(500, 200);

        _canvas = GetComponent<Canvas>();
        _btnStartRect = (RectTransform)btnStartGroup.transform;

        logoGroup.alpha = 0;
        btnStartGroup.alpha = 0;
        btnSettingsGroup.alpha = 0;

        Sequence seq = DOTween.Sequence();

        seq.Append(logoGroup.DOFade(1f, 1.2f));
        seq.Join(logoGroup.transform.DOLocalMoveY(80f, 1.2f)
            .From(180f)
            .SetEase(Ease.OutCubic));

        seq.AppendInterval(0.3f);

        seq.Append(btnStartGroup.DOFade(1f, 0.6f));
        seq.AppendInterval(0.15f);
        seq.Append(btnSettingsGroup.DOFade(1f, 0.6f));
    }

    void Update()
    {
        if (_isLoading) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (btnStartGroup.alpha < 0.1f) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (RectTransformUtility.RectangleContainsScreenPoint(_btnStartRect, mousePos, _canvas.worldCamera))
        {
            OnClickStart();
        }
    }

    public void OnClickStart()
    {
        if (_isLoading) return;
        _isLoading = true;
        Debug.Log("[TitleManager] OnClickStart 호출됨!");
        StartCoroutine(LoadAuthScene());
    }

    private System.Collections.IEnumerator LoadAuthScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync("AuthScene", LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError("[TitleManager] AuthScene 로드 실패 - Build Settings 확인 필요");
            _isLoading = false;
            yield break;
        }
        yield return op;
        Debug.Log("[TitleManager] AuthScene 로드 완료");
    }

    public void OnClickSettings()
    {
        Debug.Log("설정 클릭");
    }
}
