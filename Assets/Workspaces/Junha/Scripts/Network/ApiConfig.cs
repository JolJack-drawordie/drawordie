using UnityEngine;
using UnityEngine.Networking;

// 게임 서버 주소를 한 곳에서 관리한다.
// 빌드한 게임은 항상 배포 서버(Render)를 쓰고, 에디터에서는 Tools > Server 메뉴로 로컬 서버 / 배포 서버를 고른다 (기본: 로컬).
public static class ApiConfig
{
    private const string LocalUrl = "http://localhost:8080";
    private const string DeployUrl = "https://drawordie-server.onrender.com";

    // 모든 API 요청 주소의 앞부분
    public static string BaseUrl
    {
        get
        {
#if UNITY_EDITOR
            return UseLocalServer ? LocalUrl : DeployUrl;
#else
            return DeployUrl;
#endif
        }
    }

    // 예: ApiConfig.Url("/api/game/save")
    public static string Url(string path) => BaseUrl + path;

    // 무료 서버는 한동안 접속이 없으면 잠들어서 첫 요청이 30초~1분 걸린다.
    // 타이틀 화면에서 미리 한 번 요청을 보내 깨워 두면, 로그인을 누를 때쯤엔 바로 응답한다. (결과는 기다리지 않음)
    public static void WakeUpServer()
    {
        UnityWebRequest request = UnityWebRequest.Get(Url("/api/users/ping"));
        request.SendWebRequest().completed += _ => request.Dispose();
    }

#if UNITY_EDITOR
    private const string UseLocalKey = "ApiConfig.UseLocalServer";
    private const string LocalMenu = "Tools/Server/로컬 서버 (localhost:8080)";
    private const string DeployMenu = "Tools/Server/배포 서버 (Render)";

    // 에디터에서 로컬 서버를 쓸지 (이 컴퓨터에만 저장)
    public static bool UseLocalServer
    {
        get => UnityEditor.EditorPrefs.GetBool(UseLocalKey, true);
        set => UnityEditor.EditorPrefs.SetBool(UseLocalKey, value);
    }

    [UnityEditor.MenuItem(LocalMenu)]
    private static void SelectLocal()
    {
        UseLocalServer = true;
        Debug.Log("[ApiConfig] 에디터 서버: 로컬 " + LocalUrl);
    }

    [UnityEditor.MenuItem(DeployMenu)]
    private static void SelectDeploy()
    {
        UseLocalServer = false;
        Debug.Log("[ApiConfig] 에디터 서버: 배포 " + DeployUrl);
    }

    // 지금 고른 쪽에 체크 표시
    [UnityEditor.MenuItem(LocalMenu, true)]
    private static bool ValidateLocal()
    {
        UnityEditor.Menu.SetChecked(LocalMenu, UseLocalServer);
        UnityEditor.Menu.SetChecked(DeployMenu, !UseLocalServer);
        return true;
    }

    [UnityEditor.MenuItem(DeployMenu, true)]
    private static bool ValidateDeploy()
    {
        UnityEditor.Menu.SetChecked(LocalMenu, UseLocalServer);
        UnityEditor.Menu.SetChecked(DeployMenu, !UseLocalServer);
        return true;
    }
#endif
}
