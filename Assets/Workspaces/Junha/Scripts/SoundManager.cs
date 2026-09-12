using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("오디오 소스 (스피커 역할)")]
    public AudioSource sfxSource; // 효과음
    public AudioSource bgmSource; // 배경음

    [Header("효과음 파일들")]
    public AudioClip drawCardSound;
    public AudioClip cardHoverSound;
    public AudioClip equipSlotSound;
    public AudioClip equipFailSound;
    public AudioClip diceRollSound;

    [Header("배경음악 파일들")]
    public AudioClip mainBackgroundSound;
    public AudioClip mapBackgroundSound;
    public AudioClip battleBackgroundSound;

    private void Awake()
    {
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

    // 효과음 재생 함수
    public void PlaySFX(AudioClip clip, bool randomizePitch = false)
    {
        if (clip != null && sfxSource != null)
        {
            if (randomizePitch)
            {
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
            }
            else
            {
                sfxSource.pitch = 1f;
            }

            sfxSource.PlayOneShot(clip);
        }
    }

    // 배경음악 재생 함수
    public void PlayBGM(AudioClip clip)
    {
        if (clip != null && bgmSource != null)
        {
            if (bgmSource.clip == clip) return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    // 배경음악 정지 함수
    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    // ==========================================
    // [추가된 음량 조절 함수들]
    // ==========================================

    // 전체 마스터 볼륨 설정
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    // BGM 및 SFX 개별 볼륨 제어 함수 (필요 시 확장)
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null) bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
    }
}