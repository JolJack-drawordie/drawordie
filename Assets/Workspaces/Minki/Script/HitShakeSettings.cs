using UnityEngine;

// 타격 흔들림(HitShake) 설정. Resources/HitShakeSettings 한 곳에서 관리.
// 에셋이 없어도 아래 기본값으로 동작함. 세기/시간 범위는 두 경우 공통, 피해량 범위만 따로.
// 내가 적을 때릴 때(기본): 피해 5 이하 → 0.03, 10 → 0.11, 15 → 0.19, 20 → 0.27, 25 이상 → 0.35
// 적이 나를 때릴 때(기본): 피해 3 → 0.11, 6 → 0.19, 9 → 0.27, 12 이상 → 0.35   (보스 처치 대폭발은 0.4)
[CreateAssetMenu(fileName = "HitShakeSettings", menuName = "DrawOrDie/Hit Shake Settings")]
public class HitShakeSettings : ScriptableObject
{
    public const string ResourcePath = "HitShakeSettings";

    [Header("내가 적을 때릴 때")]
    [Tooltip("끄면 적을 때려도 화면이 흔들리지 않음")]
    public bool shakeOnEnemyHit = true;
    [Tooltip("이 피해량 이하는 최소 세기")]
    public int minDamage = 5;
    [Tooltip("이 피해량 이상은 최대 세기")]
    public int maxDamage = 25;

    [Header("적이 나를 때릴 때")]
    [Tooltip("끄면 적에게 맞아도 화면이 흔들리지 않음")]
    public bool shakeOnPlayerHit = true;
    [Tooltip("이 피해량 이하는 최소 세기")]
    public int playerMinDamage = 0;
    [Tooltip("이 피해량 이상은 최대 세기")]
    public int playerMaxDamage = 12;

    [Header("흔들림 세기 (월드 단위, 카메라 세로 절반 = 5)")]
    public float minAmplitude = 0.03f;
    public float maxAmplitude = 0.35f;

    [Header("흔들림 시간 (실제 시간, 초)")]
    public float minDuration = 0.12f;
    public float maxDuration = 0.3f;

    [Tooltip("1 = 피해량에 정비례. 1보다 크면 작은 피해는 약하게, 큰 피해일수록 확 세짐")]
    [Range(0.3f, 3f)] public float curvePower = 1f;

    private static HitShakeSettings cached;

    public static HitShakeSettings Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<HitShakeSettings>(ResourcePath);
            if (cached == null) cached = CreateInstance<HitShakeSettings>(); // 기본값으로 동작
        }
        return cached;
    }

    public void EvaluateEnemyHit(int damage, out float amplitude, out float duration)
    {
        Evaluate(damage, minDamage, maxDamage, out amplitude, out duration);
    }

    public void EvaluatePlayerHit(int damage, out float amplitude, out float duration)
    {
        Evaluate(damage, playerMinDamage, playerMaxDamage, out amplitude, out duration);
    }

    private void Evaluate(int damage, int dMin, int dMax, out float amplitude, out float duration)
    {
        float t = Mathf.InverseLerp(dMin, Mathf.Max(dMin + 1, dMax), damage);
        t = Mathf.Pow(t, curvePower);
        amplitude = Mathf.Lerp(minAmplitude, maxAmplitude, t);
        duration = Mathf.Lerp(minDuration, maxDuration, t);
    }
}
