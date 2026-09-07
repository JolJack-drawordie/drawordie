using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup logoGroup;
    public CanvasGroup btnStartGroup;
    public CanvasGroup btnSettingsGroup;

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
        // 배경음악 재생
        if (SoundManager.Instance != null && SoundManager.Instance.mainBackgroundSound != null) {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.mainBackgroundSound);
        }

        DOTween.SetTweensCapacity(500, 200);

        // 초기 투명도 설정
        logoGroup.alpha = 0;
        btnStartGroup.alpha = 0;
        btnSettingsGroup.alpha = 0;

        // DOTween 애니메이션 시퀀스 실행
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

    // 버튼에서 직접 호출될 시작 함수
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