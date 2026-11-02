using UnityEngine;
using UnityEngine.EventSystems;

// 카드에 마우스를 올리면 호버 효과음을 재생한다.
public class CardHoverSound : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SoundManager.Instance != null && SoundManager.Instance.cardHoverSound != null)
        {
            // 같은 소리가 반복돼도 덜 거슬리도록 피치를 살짝 랜덤하게 바꿔 재생
            SoundManager.Instance.PlaySFX(SoundManager.Instance.cardHoverSound, true);
        }
    }
}