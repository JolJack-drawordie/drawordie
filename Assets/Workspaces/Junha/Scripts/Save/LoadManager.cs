using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// 로비의 '불러오기': 서버 세이브를 받아 진행 상황을 복원하고, 저장했던 곳(휴식 / 전투 씬)으로 이동한다.
// 덱과 적 상태는 전투 씬이 시작될 때 PendingLoadData에서 꺼내 복원한다. 첫 씬 로드 전에 자동으로 만들어진다.
public class LoadManager : MonoBehaviour
{
    public static LoadManager Instance;

    private static string LoadUrl => ApiConfig.Url("/api/game/load");
    private const string BattleSceneName = "BattleScene";
    private const string RestSceneName = "RestScene";

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

    // onFailed: 불러오기에 실패했을 때 플레이어에게 보여줄 문구를 받는 함수
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
            // 유저 번호는 서버가 토큰에서 꺼내 쓴다
            AuthManager.SetAuthHeader(www);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                GameSaveDto data = JsonUtility.FromJson<GameSaveDto>(www.downloadHandler.text);
                ApplyLoadedData(data);
            }
            else if (www.responseCode == 404)
            {
                // 세이브가 없음 (저장한 적이 없거나, 게임이 끝나 세이브가 지워짐)
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

    // 받은 세이브를 적용하고 씬 이동
    private void ApplyLoadedData(GameSaveDto data)
    {
        // 맵 / 노드 시드와 위치 복원
        GameFlowData.SetMasterSeed(data.masterSeed);
        GameFlowData.SetMapSeed(data.mapSeed);
        GameFlowData.currentNodeSeed = data.nodeSeed;
        GameFlowData.currentFloor = data.currentFloor;
        GameFlowData.currentIndex = data.currentIndex;
        // 예전 세이브(act 값이 없던 시절)는 0으로 오므로 1로 보정
        GameFlowData.currentAct = data.currentAct > 0 ? data.currentAct : 1;
        // 불러온 Act의 섬은 이미 열린 상태 → 다음 Act로 넘어갈 때 월드맵에서 안개 걷힘 연출
        GameFlowData.worldMapRevealedAct = GameFlowData.currentAct;
        GameFlowData.currentNodeType = (MapNode.NodeType)data.currentNodeType;
        GameFlowData.hasRested = data.rested;
        GameFlowData.eliteKills = data.eliteKills;
        GameFlowData.debugUnlockBoss = false; // 불러온 게임에는 테스트 설정을 적용하지 않음

        // 저장할 때의 플레이 타임부터 이어서 잰다
        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.ResumeRun(data.playTime);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        // 보유 유물과 최대 체력 복원 (휴식 / 전투 어느 쪽이든 먼저 적용)
        RelicSaveData relics = string.IsNullOrEmpty(data.relicData)
            ? null
            : JsonUtility.FromJson<RelicSaveData>(data.relicData);
        RelicManager.RestoreOwnedRelics(relics != null ? relics.relicKeys : null);

        if (StatManager.Instance != null)
            StatManager.Instance.SetPlayerMaxHp(data.maxHp);

        // 휴식 노드에서 저장한 경우: 체력은 바로 복원하고, 덱은 다음 전투가 시작될 때 복원
        if (GameFlowData.currentNodeType == MapNode.NodeType.Rest)
        {
            if (StatManager.Instance != null && StatManager.Instance.runtimePlayerStat != null)
            {
                StatManager.Instance.runtimePlayerStat.currentHp = data.currentHp;
                StatManager.Instance.runtimePlayerStat.currentShield = 0;
            }

            PendingLoadData.Clear();
            PendingLoadData.pendingDeckJson = data.deckData;

            Debug.Log("<color=green>[LoadManager] 세이브 데이터 불러오기 완료! 휴식 씬으로 이동합니다.</color>");
            SceneManager.LoadScene(RestSceneName);
            return;
        }

        // 전투 중 저장한 경우: 전투 씬에서 꺼내 쓸 수 있게 맡겨 두고 이동
        PendingLoadData.pendingDeckJson = null;
        PendingLoadData.Set(data.currentHp, data.currentShield, data.currentCost, data.maxCost, data.deckData, data.monsterData);

        Debug.Log("<color=green>[LoadManager] 세이브 데이터 불러오기 완료! 전투 씬으로 이동합니다.</color>");
        SceneManager.LoadScene(BattleSceneName);
    }
}
