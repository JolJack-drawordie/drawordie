using UnityEngine;

// 게임 일시정지. Time.timeScale을 0으로 만들어 코루틴(WaitForSeconds) / deltaTime 이동 / 애니메이터 / 파티클 이펙트를
// 그 자리에서 멈추고, 플레이 타임 측정도 멈춘다. 설정 창처럼 UI 이벤트는 시간과 무관하게 계속 동작한다.
public static class GamePause
{
    public static bool IsPaused { get; private set; }

    private static float timeScaleBeforePause = 1f;
    private static bool timerPausedBeforePause;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        IsPaused = false;
        timeScaleBeforePause = 1f;
        timerPausedBeforePause = false;
    }

    public static void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        if (PlayTimeTracker.Instance != null)
        {
            timerPausedBeforePause = PlayTimeTracker.Instance.IsPaused;
            PlayTimeTracker.Instance.IsPaused = true;
        }
    }

    public static void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = timeScaleBeforePause;

        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.IsPaused = timerPausedBeforePause;
    }
}
