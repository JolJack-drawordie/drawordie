using UnityEngine;

// 카메라 흔들림 공용 처리. 타격 흔들림(HitShake)과 보스 처치 연출(BossDeathSequence)이 동시에 흔들어도
// 서로 카메라 위치를 덮어쓰지 않도록 한 곳에서만 카메라를 움직인다.
// - 흔든 만큼만 더했다가 다음 프레임에 빼므로, 다른 스크립트가 카메라를 옮겨도 원위치가 꼬이지 않음
// - 실제 시간 기준이라 히트스톱 / 슬로모 중에도 같은 속도로 흔들리고, 일시정지(GamePause) 중에는 멈춤
// - 씬에 둘 필요 없음. 처음 Shake()가 불릴 때 자동으로 만들어짐
public class CameraShaker : MonoBehaviour
{
    private static CameraShaker instance;

    private Transform cam;
    private Vector3 appliedOffset; // 지난 프레임에 카메라에 더한 흔들림
    private float amplitude;
    private float duration;
    private float timer;

    // 도메인 리로드를 꺼 둔 에디터에서도 플레이할 때마다 처음 상태로
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    // amplitude: 최대 흔들림 거리(월드 단위, 카메라 세로 절반 = 5), duration: 실제 시간(초)
    // 이미 더 센 흔들림이 진행 중이면 약한 흔들림은 무시 (센 연출이 끊기지 않게)
    public static void Shake(float amplitude, float duration)
    {
        if (amplitude <= 0f || duration <= 0f) return;
        Ensure().AddShake(amplitude, duration);
    }

    // 진행 중인 흔들림 즉시 종료
    public static void Stop()
    {
        if (instance != null) instance.timer = 0f;
    }

    private static CameraShaker Ensure()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("CameraShaker");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CameraShaker>();
        }
        return instance;
    }

    // 지금 남아 있는 흔들림 세기 (시간이 지날수록 약해짐)
    private float CurrentStrength()
    {
        return timer > 0f ? amplitude * (timer / Mathf.Max(0.01f, duration)) : 0f;
    }

    private void AddShake(float amp, float dur)
    {
        if (amp < CurrentStrength()) return;
        amplitude = amp;
        duration = dur;
        timer = dur;
    }

    private void LateUpdate()
    {
        Transform target = Camera.main != null ? Camera.main.transform : null;
        if (target != cam)
        {
            // 씬 전환 등으로 카메라가 바뀌면 이전 카메라에 남은 흔들림을 되돌리고 새 카메라로
            if (cam != null) cam.localPosition -= appliedOffset;
            appliedOffset = Vector3.zero;
            cam = target;
        }
        if (cam == null || GamePause.IsPaused) return;

        // 지난 프레임에 더한 만큼 되돌린 뒤 새로 흔든다
        cam.localPosition -= appliedOffset;
        appliedOffset = Vector3.zero;

        if (timer <= 0f) return;
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            return;
        }

        float k = timer / Mathf.Max(0.01f, duration); // 점점 약해짐
        Vector2 o = Random.insideUnitCircle * amplitude * k;
        appliedOffset = new Vector3(o.x, o.y, 0f);
        cam.localPosition += appliedOffset;
    }
}
