using UnityEngine;

// 보스 처치 연출(BossDeathSequence) 설정. 프리팹마다 넣지 않고 Resources/BossDeathFxSettings 한 곳에서 관리.
// 에셋이 없거나 이펙트/효과음이 비어 있어도 연출(히트스톱, 슬로모, 화면 번쩍, 흔들림, 페이드)은 그대로 동작함.
[CreateAssetMenu(fileName = "BossDeathFxSettings", menuName = "DrawOrDie/Boss Death FX Settings")]
public class BossDeathFxSettings : ScriptableObject
{
    public const string ResourcePath = "BossDeathFxSettings";

    [Header("적용 대상")]
    public bool playOnBoss = true;
    [Tooltip("중간보스(Elite)도 BossController2를 쓰므로, 켜면 중간보스 처치 때도 재생 (기본 켜짐)")]
    public bool playOnElite = true;

    [Header("1) 히트스톱 - 마지막 타격 순간 멈칫")]
    [Range(0f, 1f)] public float hitStopTimeScale = 0.03f;
    public float hitStopDuration = 0.18f;          // 실제 시간(초)
    public AudioClip deathCrySound;               // 단말마 (비우면 생략)

    [Header("2) 슬로모 + 연쇄 폭발")]
    [Range(0.05f, 1f)] public float slowMoTimeScale = 0.4f;
    public GameObject chainExplosionPrefab;
    public int chainCount = 4;
    public float chainInterval = 0.22f;            // 실제 시간(초)
    [Tooltip("연쇄 폭발 크기 = 몸 높이 x 이 값")]
    public float chainExplosionSize = 0.45f;
    public AudioClip chainExplosionSound;         // 비우면 SoundManager.enemyHitSound
    public Color chainTint = new Color(1f, 0.45f, 0.4f, 1f);

    [Header("3) 마무리 대폭발 + 소멸")]
    public GameObject finalExplosionPrefab;
    [Tooltip("대폭발 크기 = 몸 높이 x 이 값")]
    public float finalExplosionSize = 1.4f;
    public GameObject finalSmokePrefab;
    public float finalSmokeSize = 1.1f;
    public AudioClip finalExplosionSound;
    public float bodyFadeDuration = 0.45f;

    [Header("화면 연출")]
    public Color screenFlashColor = new Color(1f, 1f, 1f, 0.85f);
    public float screenFlashDuration = 0.3f;
    public float hitShake = 0.25f;
    public float chainShake = 0.12f;
    public float finalShake = 0.4f;
    public float shakeDuration = 0.35f;

    [Header("마무리")]
    [Tooltip("소멸 후 결과/보상 화면이 뜨기 전까지 정적")]
    public float endDelay = 0.6f;
    [Tooltip("연출 중 카드/버튼 클릭 막기")]
    public bool blockInput = true;

    private static BossDeathFxSettings cached;

    public static BossDeathFxSettings Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<BossDeathFxSettings>(ResourcePath);
            if (cached == null) cached = CreateInstance<BossDeathFxSettings>(); // 기본값으로 동작
        }
        return cached;
    }

    public bool ShouldPlayFor(MapNode.NodeType nodeType)
    {
        if (nodeType == MapNode.NodeType.Boss) return playOnBoss;
        if (nodeType == MapNode.NodeType.Elite) return playOnElite;
        return false;
    }
}
