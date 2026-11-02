using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 플레이어 체력이 낮을 때(기본 10 이하) 화면 테두리를 붉게 덮는 경고 연출 (URP 후처리 Vignette). 설정: Resources/LowHpVignetteSettings
// - 씬에 둘 필요 없음. 게임 시작 시 자동으로 만들어지고 씬이 바뀌어도 유지
// - 전투 중(GameManager.currentPlayer가 있을 때)에만 표시하고, 체력이 회복되면 서서히 꺼짐
// - 전투 카메라는 후처리가 꺼져 있어서, 표시되는 동안만 켜고 다 꺼지면 원래대로 되돌림
// - 후처리라 오버레이 UI(카드, 체력 바 등)는 덮지 않고 그 아래 게임 화면에만 적용됨
public class LowHpVignette : MonoBehaviour
{
    private static LowHpVignette instance;

    private LowHpVignetteSettings settings;
    private Volume volume;
    private VolumeProfile profile;
    private Vignette vignette;
    private float weight; // 0(꺼짐) ~ 1(완전히 표시)

    private UniversalAdditionalCameraData camData;
    private bool camPostOriginal; // 후처리를 켜기 전 카메라 설정 (되돌릴 때 사용)

    // 도메인 리로드를 꺼 둔 에디터에서도 플레이할 때마다 처음 상태로
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    // 첫 씬 로드 후 한 번 자동 생성
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

        // 코드로 Vignette 하나만 들어 있는 볼륨 프로필을 만든다
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
        // 표시 여부에 따라 서서히 켜고 끄며, 켜져 있는 동안은 맥박처럼 세기가 오르내린다
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

    // 지금 메인 카메라의 후처리를 켠다 (카메라가 바뀌었으면 이전 카메라는 원래대로)
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

    // 후처리 설정을 켜기 전 상태로 되돌림
    private void RestoreCamera()
    {
        if (camData != null) camData.renderPostProcessing = camPostOriginal;
        camData = null;
    }
}
