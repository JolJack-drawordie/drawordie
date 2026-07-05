using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverSound : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. 매니저가 있는지 확인 2. 소리 파일이 있는지 확인 3. 소리 재생
        if (SoundManager.Instance != null && SoundManager.Instance.cardHoverSound != null)
        {
            // 랜덤 피치를 적용하여 매번 미세하게 다른 소리가 나게 함 (귀가 피로하지 않음)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.cardHoverSound, true);
        }
    }
}