using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public enum MonsterType
{
    Slime = 100,
    Rat = 101,
    Ghost = 102
}

[System.Serializable]
public class MonsterPrefabInfo
{
    public MonsterType monsterType; // 또는 MonsterType monsterType
    public GameObject monsterPrefab;
}

public class MonsterDatabase : MonoBehaviour
{
    public static MonsterDatabase Instance;

    [Header("프리팹 매핑 전용 (도감)")]
    public List<MonsterPrefabInfo> prefabList = new List<MonsterPrefabInfo>();

    // 런타임에 서버에서 받아온 스탯 데이터를 따로 보관
    private Dictionary<int, MonsterServerData> serverStatDict = new Dictionary<int, MonsterServerData>();

    public bool IsDataLoaded { get; private set; } = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        GameObject prefab = Resources.Load<GameObject>("MonsterDatabase");
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);
        }
        else
        {
            GameObject obj = new GameObject("MonsterDatabase");
            obj.AddComponent<MonsterDatabase>();
            DontDestroyOnLoad(obj);
        }
    }

    private void Awake()
    {
        // 이미 인스턴스가 존재하는데 새로 생성된 경우, 중복 생성 방지
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(LoadServerDataFromJson());
    }

    // 프리팹을 가져오는 함수
    public GameObject GetPrefab(MonsterType type)
    {
        var info = prefabList.Find(x => x.monsterType == type);
        return info?.monsterPrefab;
    }

    // 서버 스탯만 반환
    public MonsterServerData GetServerStat(int id)
    {
        if (serverStatDict.TryGetValue(id, out var stat))
        {
            return stat;
        }
        Debug.LogWarning($"ID {id}에 해당하는 서버 스탯 정보를 찾을 수 없습니다.");
        return null;
    }

    public bool TryInitializeMonsterStat(int monsterId)
    {
        if (serverStatDict.TryGetValue(monsterId, out var serverData))
        {
            // 데이터베이스가 알아서 가져와서 타겟(스탯 매니저)에 꽂아줌
            StatManager.Instance.SetEnemyStat(serverData.hp, serverData.shield);
            return true;
        }

        Debug.LogError($"ID {monsterId}에 해당하는 서버 스탯이 없습니다.");
        return false;
    }

    // 서버에서 받은 JSON 문자열을 파싱해서 데이터베이스에 일괄 적용
    public IEnumerator LoadServerDataFromJson()
    {
        // 몬스터 스탯을 가져올 서버 API 주소
        string monsterUrl = "http://localhost:8080/api/game/load-monsters";

        yield return StartCoroutine(FetchData<MonsterServerDataListWrapper>(monsterUrl, (wrapper) => {
            if (wrapper == null || wrapper.monsters == null)
            {
                Debug.LogError("몬스터 데이터 리스트가 비어있거나 파싱에 실패했습니다.");
                return;
            }

            serverStatDict.Clear();
            foreach (var serverData in wrapper.monsters)
            {
                serverStatDict[serverData.id] = serverData;
                Debug.Log($"[서버 스탯 캐싱 완료] ID: {serverData.id}, 이름: {serverData.name}, HP: {serverData.hp}, Shield: {serverData.shield}");
            }

            IsDataLoaded = true;
        }));

        Debug.Log("모든 몬스터 데이터 로딩 및 데이터베이스 갱신 완료!");
    }

    // 2. 공통 FetchData 메서드
    private IEnumerator FetchData<T>(string url, System.Action<T> onSuccess) where T : class
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"통신 실패 ({url}): {request.error}");
                yield break;
            }

            string jsonString = request.downloadHandler.text;

            // 만약 서버가 배열형태로 바로 준다면 래퍼 처리가 필요할 수 있음
            string wrappedJson = "{\"monsters\":" + jsonString + "}";
            T data = JsonUtility.FromJson<T>(wrappedJson);

            onSuccess?.Invoke(data);
        }
    }
}