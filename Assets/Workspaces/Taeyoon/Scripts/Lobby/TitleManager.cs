using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

// 타이틀 화면: 로고와 버튼이 차례로 나타나는 연출, 시작 버튼을 누르면 로그인 팝업(AuthScene)을 위에 연다.
public class TitleManager : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup logoGroup;
    public CanvasGroup btnStartGroup;
    public CanvasGroup btnSettingsGroup;

    private bool _isLoading = false; // 로그인 팝업을 여는 중 / 열려 있음 (중복으로 열지 않게)

    void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    // 로그인 팝업이 닫히면 다시 시작 버튼을 누를 수 있게
    void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "AuthScene")
            _isLoading = false;
    }

    void Start()
    {
        // 메인 BGM
        if (SoundManager.Instance != null && SoundManager.Instance.mainBackgroundSound != null) {
            SoundManager.Instance.PlayBGM(SoundManager.Instance.mainBackgroundSound);
        }

        DOTween.SetTweensCapacity(500, 200); // 화면 연출 트윈이 많아서 미리 용량 확보

        // 처음엔 모두 투명
        logoGroup.alpha = 0;
        btnStartGroup.alpha = 0;
        btnSettingsGroup.alpha = 0;

        // 로고가 위에서 내려오며 나타난 뒤 → 시작 버튼 → 설정 버튼 순서로
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

    // 시작 버튼 (onClick에 연결): 로그인 팝업 열기
    public void OnClickStart()
    {
        if (_isLoading) return;
        _isLoading = true;
        Debug.Log("[TitleManager] OnClickStart 호출됨!");
        StartCoroutine(LoadAuthScene());
    }

    // 로그인 씬을 타이틀 위에 Additive로 불러옴
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

    // 설정 버튼 (onClick에 연결)
    public void OnClickSettings()
    {
        Debug.Log("설정 클릭");
    }
}