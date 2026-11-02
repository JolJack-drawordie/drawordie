using UnityEngine;

// 빌드한 게임의 화면을 항상 1920x1080(16:9)으로 고정한다.
// UI가 1920x1080 기준으로 배치돼 있어서, 16:10 노트북 같은 다른 비율에서는 위치가 밀리거나 화면 밖으로 나가기 때문.
// 전체 화면에서 모니터 비율이 다르면 비율을 유지한 채 남는 부분은 검은 띠로 채워진다.
// Alt+Enter로 창 모드 / 전체 화면을 바꾸는 등 해상도가 바뀌면 다시 1920x1080으로 맞춘다.
// 게임이 시작될 때 자동으로 만들어진다 (씬에 둘 필요 없음). 에디터에서는 Game 창 설정을 그대로 쓴다.
public class ResolutionLock : MonoBehaviour
{
    private const int Width = 1920;
    private const int Height = 1080;
    private const float RetryDelay = 0.5f; // 해상도 변경은 다음 프레임 이후에 반영되므로 연달아 요청하지 않음

    private float nextCheckTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (Application.isEditor)
            return;

        GameObject go = new GameObject("ResolutionLock");
        DontDestroyOnLoad(go);
        go.AddComponent<ResolutionLock>();
        Apply();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
            return;
        if (Screen.width == Width && Screen.height == Height)
            return;
        // 모니터가 1920x1080 이하면 창 모드로는 1920x1080 창이 들어가지 않으므로 계속 요청하지 않음 (전체 화면은 그대로 맞춤)
        if (Screen.fullScreenMode == FullScreenMode.Windowed && Screen.currentResolution.height <= Height)
            return;

        Apply();
        nextCheckTime = Time.unscaledTime + RetryDelay;
    }

    // 지금 화면 모드(창 / 전체 화면)는 유지하고 해상도만 1920x1080으로
    private static void Apply()
    {
        FullScreenMode mode = Screen.fullScreenMode == FullScreenMode.Windowed
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;
        Screen.SetResolution(Width, Height, mode);
    }
}
