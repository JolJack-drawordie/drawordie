using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 플레이어 체력이 낮을 때(기본 10 이하) 화면 테두리를 붉게 덮는 경고 연출. 설정: Resources/LowHpVignetteSettings
// URP 후처리의 Vignette를 사용한다.
//   - 씬에 둘 필요 없음. 게임 시작 시 자동 생성되어 씬이 바뀌어도 유지
//   - 전투 중(GameManager.currentPlayer가 있을 때)에만 표시, 체력이 회복되면 서서히 꺼짐
//   - 전투 카메라는 후처리가 꺼져 있어서, 표시되는 동안만 켜고 다 꺼지면 원래대로 되돌림
//     (기본 볼륨 프로필의 다른 효과는 모두 0이라 화면이 달라지지 않음)
//   - 후처리라서 Screen Space Overlay UI(카드, 체력바 등)는 덮지 않고 그 아래 게임 화면에만 적용됨
public class LowHpVignette : MonoBehaviour
{
    private static LowHpVignette instance;

    private LowHpVignetteSettings settings;
    private Volume volume;
    private VolumeProfile profile;
    private Vignette vignette;
    private float weight;

    private UniversalAdditionalCameraData camData;
    private bool camPostOriginal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (instance != null) return;
        GameObject go = new GameObject("LowHpVignette");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<LowHpVignette>();
    }

    private void Awake()
    {
        settings = LowHpVignetteSettings.Load();

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vignette = profile.Add<Vignette>(true);
        vignette.rounded.Override(false);

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f; // 씬에 다른 볼륨이 있어도 이 값이 우선
        volume.sharedProfile = profile;
        volume.weight = 0f;
    }

    private void OnDestroy()
    {
        RestoreCamera();
        if (profile != null) Destroy(profile);
        if (instance == this) instance = null;
    }

    private void Update()
    {
        bool show = settings.useVignette && IsPlayerLowHp();

        float fade = Mathf.Max(0.01f, show ? settings.fadeInTime : settings.fadeOutTime);
        weight = Mathf.MoveTowards(weight, show ? 1f : 0f, Time.unscaledDeltaTime / fade);

        if (weight <= 0f)
        {
            volume.weight = 0f;
            RestoreCamera();
            return;
        }

        float pulse = 0f;
        if (settings.pulseAmount > 0f)
            pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * settings.pulseSpeed) * 0.5f + 0.5f) * settings.pulseAmount;

        vignette.color.Override(settings.color);
        vignette.intensity.Override(Mathf.Clamp01(settings.intensity + pulse));
        vignette.smoothness.Override(settings.smoothness);
        volume.weight = weight;

        EnsureCameraPostProcessing();
    }

    private bool IsPlayerLowHp()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;
        PlayerUnit player = gm.currentPlayer;
        if (player == null || player.statData == null) return false;
        return player.statData.currentHp <= settings.hpThreshold;
    }

    private void EnsureCameraPostProcessing()
    {
        Camera cam = Camera.main;
        UniversalAdditionalCameraData data = cam != null ? cam.GetUniversalAdditionalCameraData() : null;
        if (data == camData) return;

        RestoreCamera(); // 카메라가 바뀌었으면 이전 카메라는 원래대로
        camData = data;
        if (camData == null) return;
        camPostOriginal = camData.renderPostProcessing;
        camData.renderPostProcessing = true;
    }

    private void RestoreCamera()
    {
        if (camData != null) camData.renderPostProcessing = camPostOriginal;
        camData = null;
    }
}
