using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

// 유닛 위에 떠오르는 숫자 하나 (피해 / 막음 / 회복 / 방어도). DamagePopupManager가 풀에서 꺼내 재사용한다.
public class DamagePopup : MonoBehaviour
{
    private TextMeshPro text;
    private Sequence seq;
    private Action<DamagePopup> release;

    // 풀에서 처음 만들어질 때 한 번 호출: 글자 모양 설정 + 끝나면 풀로 돌려보낼 함수 등록
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

    // 펀치하듯 커졌다가 원래 크기로 → 위로 떠오르며 후반 절반 동안 사라짐 → 풀로 반환
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
            .SetLink(gameObject) // 오브젝트가 사라지면 트윈도 함께 정리
            .OnComplete(() => release(this));
    }
}
