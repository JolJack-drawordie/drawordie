using UnityEngine;

// 체력 위험 경고(LowHpVignette) 설정. Resources/LowHpVignetteSettings 한 곳에서 관리.
// 에셋이 없어도 아래 기본값으로 동작함.
[CreateAssetMenu(fileName = "LowHpVignetteSettings", menuName = "DrawOrDie/Low HP Vignette Settings")]
public class LowHpVignetteSettings : ScriptableObject
{
    public const string ResourcePath = "LowHpVignetteSettings";

    [Tooltip("끄면 체력이 낮아도 표시하지 않음")]
    public bool useVignette = true;
    [Tooltip("플레이어 체력이 이 값 이하이면 화면 테두리를 붉게 덮음")]
    public int hpThreshold = 10;

    [Header("모양 (URP Vignette)")]
    public Color color = new Color(0.85f, 0f, 0f, 1f);
    [Tooltip("테두리가 안쪽으로 얼마나 들어오는지 (0~1)")]
    [Range(0f, 1f)] public float intensity = 0.4f;
    [Tooltip("테두리 경계가 얼마나 부드럽게 퍼지는지 (0~1)")]
    [Range(0.01f, 1f)] public float smoothness = 0.6f;

    [Header("맥박 (0이면 일정하게 유지)")]
    [Range(0f, 0.5f)] public float pulseAmount = 0.08f;
    [Tooltip("1초에 몇 번 두근거리는지")]
    public float pulseSpeed = 1.2f;

    [Header("켜지고 꺼지는 시간 (초)")]
    public float fadeInTime = 0.4f;
    public float fadeOutTime = 0.6f;

    private static LowHpVignetteSettings cached;

    public static LowHpVignetteSettings Load()
    {
        if (cached == null)
        {
            cached = Resources.Load<LowHpVignetteSettings>(ResourcePath);
            if (cached == null) cached = CreateInstance<LowHpVignetteSettings>(); // 기본값으로 동작
        }
        return cached;
    }
}
