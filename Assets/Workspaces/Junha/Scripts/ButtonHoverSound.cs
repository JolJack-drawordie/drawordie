using UnityEngine;

// 버튼에 붙은 onClick/EventTrigger가 AudioSource.PlayOneShot을 직접 호출하면 두 가지 문제가 있다:
// 1) 클릭으로 버튼(패널)이 같은 프레임에 비활성화되면 "Can not play a disabled audio source" 경고가 뜬다.
// 2) OpenRegister/OpenLogin처럼 "동기적으로 즉시" 패널을 꺼버리는 경우, 버튼 자신의 AudioSource도 같이
//    꺼지면서 재생을 시작한 소리 자체가 잘려서 아예 들리지 않는다.
// 그래서 패널과 무관하게 항상 살아있는 전역 SoundManager를 우선 사용하고, 없을 때만 로컬 AudioSource로 대체한다.
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
            audioSource.PlayOneShot(clip);
        }
    }
}
