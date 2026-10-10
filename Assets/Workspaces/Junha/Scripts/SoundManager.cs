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
    public AudioClip healSound;
    public AudioClip hitSound;
    public AudioClip shieldSound;

    [Header("배경음악 파일들")]
    public AudioClip mainBackgroundSound;
    public AudioClip mapBackgroundSound;
    public AudioClip battleBackgroundSound;

    [Header("Act별 배경음악 (0: Act1 숲 / 1: Act2 불 / 2: Act3 어둠, 비어 있으면 기본 BGM)")]
    public AudioClip[] actMapBackgroundSounds = new AudioClip[GameFlowData.maxAct];
    public AudioClip[] actBattleBackgroundSounds = new AudioClip[GameFlowData.maxAct];
    public AudioClip[] actBossBackgroundSounds = new AudioClip[GameFlowData.maxAct];

    [Header("휴식 배경음악")]
    public AudioClip restBackgroundSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
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
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // 현재 Act의 맵 BGM (슬롯이 비어 있으면 기본 맵 BGM)
    public AudioClip GetMapBGM()
    {
        AudioClip clip = GetActClip(actMapBackgroundSounds);
        return clip != null ? clip : mapBackgroundSound;
    }

    // 현재 노드에 맞는 전투 BGM (보스는 Act별 보스 BGM, 일반/엘리트는 Act별 전투 BGM, 슬롯이 비어 있으면 공통 전투 BGM)
    public AudioClip GetBattleBGM()
    {
        if (GameFlowData.currentNodeType == MapNode.NodeType.Boss)
        {
            AudioClip bossClip = GetActClip(actBossBackgroundSounds);
            if (bossClip != null) return bossClip;
        }

        AudioClip battleClip = GetActClip(actBattleBackgroundSounds);
        return battleClip != null ? battleClip : battleBackgroundSound;
    }

    private AudioClip GetActClip(AudioClip[] clips)
    {
        int index = GameFlowData.currentAct - 1;
        if (clips == null || index < 0 || index >= clips.Length) return null;
        return clips[index];
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

    // BGM / SFX 개별 볼륨 (0~1, PlayerPrefs에 저장)
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;
    public bool BGMEnabled { get; private set; } = true;
    public bool SFXEnabled { get; private set; } = true;

    private const string BGMVolumeKey = "BGMVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string BGMEnabledKey = "BGMEnabled";
    private const string SFXEnabledKey = "SFXEnabled";
    private const string LegacyVolumeKey = "Volume"; // 예전 통합 음량 슬라이더 값

    // SoundManager가 없는 씬에서 로컬 AudioSource로 효과음을 낼 때 쓰는 저장된 효과음 음량 (음소거면 0)
    public static float SavedSFXVolume
    {
        get
        {
            if (PlayerPrefs.GetInt(SFXEnabledKey, 1) != 1) return 0f;
            return PlayerPrefs.GetFloat(SFXVolumeKey, PlayerPrefs.GetFloat(LegacyVolumeKey, 1f));
        }
    }

    // 저장된 볼륨을 불러와 적용 (예전 통합 음량 값이 있으면 둘 다 그 값으로 시작)
    private void LoadVolumeSettings()
    {
        float legacyVolume = PlayerPrefs.GetFloat(LegacyVolumeKey, 1f);
        BGMVolume = PlayerPrefs.GetFloat(BGMVolumeKey, legacyVolume);
        SFXVolume = PlayerPrefs.GetFloat(SFXVolumeKey, legacyVolume);
        BGMEnabled = PlayerPrefs.GetInt(BGMEnabledKey, 1) == 1;
        SFXEnabled = PlayerPrefs.GetInt(SFXEnabledKey, 1) == 1;

        // 예전에는 AudioListener.volume에도 같은 값을 곱해서 실제 음량이 제곱으로 작아졌음
        AudioListener.volume = 1f;
        ApplyVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        BGMVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BGMVolumeKey, BGMVolume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SFXVolumeKey, SFXVolume);
        ApplyVolumes();
    }

    public void SetBGMEnabled(bool enabled)
    {
        BGMEnabled = enabled;
        PlayerPrefs.SetInt(BGMEnabledKey, enabled ? 1 : 0);
        ApplyVolumes();
    }

    public void SetSFXEnabled(bool enabled)
    {
        SFXEnabled = enabled;
        PlayerPrefs.SetInt(SFXEnabledKey, enabled ? 1 : 0);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null) bgmSource.volume = BGMEnabled ? BGMVolume : 0f;
        if (sfxSource != null) sfxSource.volume = SFXEnabled ? SFXVolume : 0f;
    }
}
