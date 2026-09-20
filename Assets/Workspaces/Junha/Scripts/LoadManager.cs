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

    public void LoadGame()
    {
        if (!AuthManager.isLoggedIn)
        {
            Debug.LogWarning("[LoadManager] 로그인되어 있지 않아 불러올 수 없습니다.");
            return;
        }

        StartCoroutine(LoadGameRoutine());
    }

    private IEnumerator LoadGameRoutine()
    {
        string url = LoadUrl + "?userId=" + AuthManager.userId;

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                GameSaveDto data = JsonUtility.FromJson<GameSaveDto>(www.downloadHandler.text);
                ApplyLoadedData(data);
            }
            else
            {
                Debug.LogError("[LoadManager] 불러오기 실패: " + www.error);
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

        PendingLoadData.Set(data.currentHp, data.currentShield, data.currentCost, data.deckData, data.monsterData);

        Debug.Log("<color=green>[LoadManager] 세이브 데이터 불러오기 완료! 전투 씬으로 이동합니다.</color>");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
        }

        SceneManager.LoadScene(BattleSceneName);
    }
}
