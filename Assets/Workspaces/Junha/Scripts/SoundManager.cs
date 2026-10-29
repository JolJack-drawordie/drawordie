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
    public AudioClip hitSound;          // 플레이어가 피해를 입을 때
    public AudioClip playerAttackSound; // 플레이어가 공격할 때 (비어 있으면 hitSound)
    public AudioClip shieldSound;
    public AudioClip blockSound;        // 방어도로 피해를 막았을 때 (플레이어·적 공통)
    public AudioClip deathSound;        // 플레이어 사망 시
    public AudioClip defeatSound;       // 게임 패배 징글 (사망음 뒤, 결과 화면 전에 한 번)
    public AudioClip victorySound;      // 최종 보스 처치(게임 클리어) 팡파레, 끝나면 엔딩 영상 시작

    [Header("적 효과음")]
    public AudioClip enemyAttackSound; // 적이 공격을 내려치는 순간
    public AudioClip enemyDefendSound; // 적이 방어할 때 (비어 있으면 shieldSound)
    public AudioClip enemyHitSound;    // 적이 피해를 입을 때

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

    [Header("엔딩 배경음악 (엔딩 영상 후 클리어 랭킹 화면, 비어 있으면 메인 BGM)")]
    public AudioClip endingRankingBackgroundSound;

    [Header("월드맵")]
    public AudioClip worldMapBackgroundSound;
    public AudioClip worldMapHoverSound;  // 갈 수 있는 섬에 마우스를 올림
    public AudioClip worldMapClickSound;  // 갈 수 있는 섬을 클릭
    public AudioClip worldMapLockedSound; // 잠긴 섬을 클릭 (거부)
    public AudioClip worldMapEnterSound;  // 섬으로 빨려 들어가는 연출
    public AudioClip fogCoverSound;       // 안개가 몰려와 화면을 덮음
    public AudioClip fogClearSound;       // 맵 씬에서 안개가 걷힘
    public AudioClip worldMapRevealSound; // 월드맵에서 새로 열린 섬의 안개가 걷힘
    public AudioClip worldMapStampSound;  // 방금 클리어한 섬에 클리어 도장이 찍힘
    public AudioClip worldMapPathSound;   // 다음 섬으로 점선이 그려지기 시작 (한 번)
    public AudioClip worldMapPathDotSound; // 점선의 점이 하나 찍힐 때마다 (피치 랜덤)

    private Coroutine bgmFadeRoutine;

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
            CancelBGMFade();

            // 같은 곡이라도 StopBGM으로 멈춰 있었다면 다시 재생 (예: 월드맵 → 맵 → 월드맵)
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

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

    // 배경음악을 duration초 동안 서서히 줄인 뒤 정지 (씬 전환 연출용)
    public void FadeOutBGM(float duration)
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;

        CancelBGMFade();
        bgmFadeRoutine = StartCoroutine(FadeOutBGMRoutine(duration));
    }

    private System.Collections.IEnumerator FadeOutBGMRoutine(float duration)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmFadeRoutine = null;
        ApplyVolumes(); // 다음 곡은 원래 볼륨으로
    }

    private void CancelBGMFade()
    {
        if (bgmFadeRoutine == null) return;

        StopCoroutine(bgmFadeRoutine);
        bgmFadeRoutine = null;
        ApplyVolumes();
    }

    // 배경음악 정지 함수
    public void StopBGM()
    {
        CancelBGMFade();

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
