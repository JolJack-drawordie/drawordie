using UnityEngine;

// 버튼 호버/클릭 효과음을 안전하게 재생한다. (버튼 onClick / EventTrigger에서 PlaySafely 호출)
// 버튼 자신의 AudioSource로 재생하면, 클릭과 동시에 패널이 꺼질 때 소리가 잘리거나 경고가 뜬다.
// 그래서 씬이 바뀌어도 살아 있는 SoundManager로 재생하고, 없을 때만 버튼의 AudioSource를 쓴다.
[RequireComponent(typeof(AudioSource))]
public class ButtonHoverSound : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlaySafely(AudioClip clip)
    {
        if (clip == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(clip);
            return;
        }

        if (audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clip, SoundManager.SavedSFXVolume);
        }
    }
}
