using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // 어디서든 SoundManager.Instance로 접근할 수 있게 해주는 마법의 코드 (싱글톤 패턴)
    public static SoundManager Instance;

    [Header("오디오 소스 (스피커 역할)")]
    public AudioSource sfxSource;

    [Header("효과음 파일들 (여기에 mp3/wav 파일 연결)")]
    public AudioClip drawCardSound; // 카드 뽑는 소리
    public AudioClip cardHoverSound; // 마우스 올렸을 때 소리
    public AudioClip equipSlotSound; // 슬롯 장착 소리
    public AudioClip equipFailSound; // 장착 실패(튕겨나감) 소리
    public AudioClip diceRollSound; // 주사위 굴리는 소리

    private void Awake()
    {
        // 씬이 넘어가도 사운드 매니저가 파괴되지 않도록 유지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 소리를 내고 싶을 때 부르는 함수 (randomizePitch를 true로 주면 음정이 매번 미세하게 바뀝니다!)
    public void PlaySFX(AudioClip clip, bool randomizePitch = false)
    {
        if (clip != null && sfxSource != null)
        {
            if (randomizePitch)
            {
                // 0.9 ~ 1.1 사이로 음정을 살짝 비틀어줍니다 (진짜 카드가 부딪히는 느낌)
                sfxSource.pitch = Random.Range(0.9f, 1.1f); 
            }
            else
            {
                // 평소에는 원래 음정(1.0)으로 재생
                sfxSource.pitch = 1f; 
            }
            
            sfxSource.PlayOneShot(clip);
        }
    }
}