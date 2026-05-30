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

    void Start()
    {
        DOTween.SetTweensCapacity(500, 200);

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

    public void OnClickStart()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void OnClickSettings()
    {
        Debug.Log("설정 클릭");
    }
}