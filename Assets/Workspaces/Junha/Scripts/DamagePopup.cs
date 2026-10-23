using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 피해/회복/방어도 숫자 하나. DamagePopupManager가 풀에서 꺼내 씀
public class DamagePopup : MonoBehaviour
{
    private TextMeshPro text;
    private Sequence seq;
    private Action<DamagePopup> release;

    public void Setup(TMP_FontAsset font, float fontSize, int sortingOrder, Action<DamagePopup> onRelease)
    {
        text = gameObject.AddComponent<TextMeshPro>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.sortingOrder = sortingOrder;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(30, 20, 20, 255);
        release = onRelease;
    }

    // 펀치 스케일로 등장 -> 위로 떠오르며 후반에 페이드 아웃 -> 풀로 반환
    public void Play(Vector3 pos, string msg, Color color, float scale, float duration, float riseHeight)
    {
        seq?.Kill();

        transform.position = pos;
        transform.localScale = Vector3.one * scale * 1.4f;
        text.text = msg;
        text.color = color;
        text.alpha = 1f;
        gameObject.SetActive(true);

        seq = DOTween.Sequence()
            .Append(transform.DOScale(scale, 0.15f).SetEase(Ease.OutBack))
            .Join(transform.DOMoveY(pos.y + riseHeight, duration).SetEase(Ease.OutCubic))
            .Insert(duration * 0.5f, DOTween.To(() => text.alpha, a => text.alpha = a, 0f, duration * 0.5f))
            .SetLink(gameObject)
            .OnComplete(() => release(this));
    }
}
