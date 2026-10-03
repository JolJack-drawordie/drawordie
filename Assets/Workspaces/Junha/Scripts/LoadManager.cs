using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// ⭐ [로드 기능] 신규 파일 (SaveManager의 로드 버전)
public class LoadManager : MonoBehaviour
{
    public static LoadManager Instance;

    private const string LoadUrl = "http://localhost:8080/api/game/load";
    private const string BattleSceneName = "JunhaTest";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        if (Instance != null) return;

        GameObject obj = new GameObject("LoadManager");
        obj.AddComponent<LoadManager>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // onFailed: 불러오기 실패 시 플레이어에게 보여줄 메시지를 전달받는 콜백
    public void LoadGame(System.Action<string> onFailed = null)
    {
        if (!AuthManager.isLoggedIn)
        {
            Debug.LogWarning("[LoadManager] 로그인되어 있지 않아 불러올 수 없습니다.");
            return;
        }

        StartCoroutine(LoadGameRoutine(onFailed));
    }

    private IEnumerator LoadGameRoutine(System.Action<string> onFailed)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(LoadUrl))
        {
            // 유저 번호는 서버가 토큰에서 꺼내 씀
            AuthManager.SetAuthHeader(www);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                GameSaveDto data = JsonUtility.FromJson<GameSaveDto>(www.downloadHandler.text);
                ApplyLoadedData(data);
            }
            else if (www.responseCode == 404)
            {
                // 세이브가 없음 (새 게임을 저장하지 않았거나, 게임이 끝나 세이브가 삭제됨)
                Debug.Log("[LoadManager] 저장된 데이터가 없습니다.");
                onFailed?.Invoke("저장된 데이터가 없습니다.");
            }
            else if (www.responseCode == 401)
            {
                Debug.LogError("[LoadManager] 불러오기 실패: 로그인이 만료되었습니다.");
                onFailed?.Invoke("로그인이 만료되었습니다. 다시 로그인해 주세요.");
            }
            else
            {
                Debug.LogError("[LoadManager] 불러오기 실패: " + www.error);
                onFailed?.Invoke("불러오기에 실패했습니다.");
            }
        }
    }

    private void ApplyLoadedData(GameSaveDto data)
    {
        // 맵/노드 시드 및 위치 복원 (실제 카드/몬스터 데이터 복원은 전투 씬 진입 후 처리)
        GameFlowData.SetMasterSeed(data.masterSeed);
        GameFlowData.SetMapSeed(data.mapSeed);
        GameFlowData.currentNodeSeed = data.nodeSeed;
        GameFlowData.currentFloor = data.currentFloor;
        GameFlowData.currentIndex = data.currentIndex;
        // 구버전 세이브(act 컬럼이 없던 시절)는 0으로 내려오므로 1로 보정
        GameFlowData.currentAct = data.currentAct > 0 ? data.currentAct : 1;
        GameFlowData.currentNodeType = (MapNode.NodeType)data.currentNodeType;
        GameFlowData.debugUnlockBoss = false; // 불러온 게임에는 테스트 설정을 적용하지 않음

        // 저장 시점의 플레이 타임부터 이어서 측정 (구버전 세이브는 0)
        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.ResumeRun(data.playTime);

        PendingLoadData.Set(data.currentHp, data.currentShield, data.currentCost, data.deckData, data.monsterData);

        Debug.Log("<color=green>[LoadManager] 세이브 데이터 불러오기 완료! 전투 씬으로 이동합니다.</color>");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        SceneManager.LoadScene(BattleSceneName);
    }
}
