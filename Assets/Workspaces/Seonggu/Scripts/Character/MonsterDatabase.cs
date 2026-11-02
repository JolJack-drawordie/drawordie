using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// 일반 몬스터 종류. 값 = 서버 Monster.json id이고, id / 100 = 등장하는 Act (1xx = Act 1, 2xx = Act 2, 3xx = Act 3)
public enum MonsterType
{
    Bat = 100,
    Rat = 101,
    Spider = 102,

    Imp = 200, // Zombie에서 교체
    Vampire = 201,
    Succubus = 202,

    SkeletonWarrior = 300,
    SkeletonMage = 301,
    SkeletonArcher = 302,
}

// 중간보스 / 보스 id 규칙 (MonsterType에는 넣지 않음 → 일반 몬스터 뽑기에서 자동 제외)
// x40 = Act x 중간보스, x50 = Act x 보스 (예: 140 = Act 1 중간보스, 350 = Act 3 보스)

// 몬스터 종류 ↔ 프리팹
[System.Serializable]
public class MonsterPrefabInfo
{
    public MonsterType monsterType;
    public GameObject monsterPrefab;
}

// 서버 몬스터 스탯과 일반 몬스터 프리팹을 관리하고, 노드 시드로 이번 Act의 몬스터를 고른다.
// 첫 씬 로드 전에 자동으로 만들어지고 씬이 바뀌어도 유지된다.
public class MonsterDatabase : MonoBehaviour
{
    public static MonsterDatabase Instance;

    // id를 Act로 바꾸는 기준 (id / 100)
    public const int IdPerAct = 100;

    [Header("프리팹 매핑 전용 (도감)")] // 비워 두면 Resources/Monsters 프리팹으로 자동 채움
    public List<MonsterPrefabInfo> prefabList = new List<MonsterPrefabInfo>();

    // 서버에 아직 없는 몬스터의 임시 기본 스탯 (서버에 같은 id가 있으면 서버 값이 우선)
    private static readonly List<MonsterServerData> localMonsters = new List<MonsterServerData>
    {
        new MonsterServerData { id = 200, name = "Imp",      hp = 40, shield = 30 },
        new MonsterServerData { id = 201, name = "Vampire",  hp = 40, shield = 30 },
        new MonsterServerData { id = 202, name = "Succubus", hp = 35, shield = 30 },

        new MonsterServerData { id = 300, name = "SkeletonWarrior", hp = 55, shield = 30 },
        new MonsterServerData { id = 301, name = "SkeletonMage",    hp = 45, shield = 30 },
        new MonsterServerData { id = 302, name = "SkeletonArcher",  hp = 50, shield = 30 },

        // 중간보스 (x40)
        new MonsterServerData { id = 140, name = "Act1Elite", hp = 70,  shield = 30 },
        new MonsterServerData { id = 240, name = "Act2Elite", hp = 95,  shield = 40 },
        new MonsterServerData { id = 340, name = "Act3Elite", hp = 120, shield = 50 },

        // 보스 (x50)
        new MonsterServerData { id = 150, name = "Act1Boss", hp = 100, shield = 30 },
        new MonsterServerData { id = 250, name = "Act2Boss", hp = 140, shield = 40 },
        new MonsterServerData { id = 350, name = "Act3Boss", hp = 200, shield = 60 },
    };

    // 서버에서 받은 몬스터 스탯 (id → 스탯)
    private Dictionary<int, MonsterServerData> serverStatDict = new Dictionary<int, MonsterServerData>();

    // Resources의 MonsterDatabase 프리팹을 만들고, 없으면 빈 오브젝트에 붙여서 만든다
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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 프리팹 목록이 비어 있으면 Resources 폴더에서 자동으로 채움
            AutoPopulatePrefabs();
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

    // Resources/Monsters의 프리팹을 훑어 이름(또는 "종류Controller" 컴포넌트)으로 몬스터 종류와 짝지음
    private void AutoPopulatePrefabs()
    {
        if (prefabList == null)
        {
            prefabList = new List<MonsterPrefabInfo>();
        }

        // 인스펙터에서 직접 등록해 둔 게 있으면 그대로 사용
        if (prefabList.Count > 0) return;

        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>("Monsters");

        foreach (var prefab in loadedPrefabs)
        {
            // 전투 유닛(EnemyUnit)이 없는 프리팹(예: Rat_Charater 같은 외형 전용 프리팹)은 제외
            if (prefab.GetComponent<EnemyUnit>() == null) continue;

            foreach (MonsterType type in System.Enum.GetValues(typeof(MonsterType)))
            {
                string typeName = type.ToString();

                // 프리팹 이름에 종류 이름이 들어 있거나(대소문자 무시, 예: rat → Rat) 같은 이름의 컨트롤러가 붙어 있으면 짝
                bool isMatch = prefab.name.IndexOf(typeName, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                               prefab.GetComponent(typeName + "Controller") != null;

                if (isMatch)
                {
                    // 종류마다 하나만
                    if (!prefabList.Exists(x => x.monsterType == type))
                    {
                        prefabList.Add(new MonsterPrefabInfo
                        {
                            monsterType = type,
                            monsterPrefab = prefab
                        });
                        Debug.Log($"[자동 매핑 완료] MonsterType: {type} -> Prefab: {prefab.name}");
                    }
                }
            }
        }
    }

    // 몬스터 종류의 프리팹
    public GameObject GetPrefab(MonsterType type)
    {
        var info = prefabList.Find(x => x.monsterType == type);
        return info?.monsterPrefab;
    }

    // id에 해당하는 서버 스탯을 StatManager의 적 스탯으로 넣는다 (id / 보스 기믹 포함)
    public bool TryInitializeMonsterStat(int monsterId)
    {
        if (serverStatDict.TryGetValue(monsterId, out var serverData))
        {
            StatManager.Instance.SetEnemyStat(serverData);
            return true;
        }

        Debug.LogError($"ID {monsterId}에 해당하는 서버 스탯이 없습니다.");
        return false;
    }

    // 이 Act에 나오는 일반 몬스터 id 목록 (항상 오름차순: 같은 시드면 같은 결과가 나오도록)
    public List<int> GetMonsterIdsForAct(int act)
    {
        List<int> result = new List<int>();

        foreach (int id in serverStatDict.Keys)
        {
            if (id / IdPerAct != act) continue;

            // 보스 id(150, 250 등)처럼 MonsterType에 없거나 프리팹이 없는 id는 일반 몬스터 풀에서 제외
            if (!System.Enum.IsDefined(typeof(MonsterType), id) || GetPrefab((MonsterType)id) == null) continue;

            result.Add(id);
        }

        result.Sort();
        return result;
    }

    // 지금 Act 기준으로 뽑기
    public int GetRandomMonsterId(int seed)
    {
        return GetRandomMonsterId(seed, GameFlowData.currentAct);
    }

    // 시드로 이 Act의 몬스터 중 하나를 고른다 (같은 시드면 항상 같은 몬스터). 없으면 Act 1 몬스터로 대신함
    public int GetRandomMonsterId(int seed, int act)
    {
        List<int> pool = GetMonsterIdsForAct(act);

        if (pool.Count == 0)
        {
            Debug.LogWarning($"[MonsterDatabase] Act {act}에 등록된 몬스터가 없어 Act 1 몬스터로 대체합니다.");
            pool = GetMonsterIdsForAct(1);
        }

        if (pool.Count == 0)
        {
            Debug.LogError("서버에서 불러온 몬스터 데이터가 없습니다!");
            return -1;
        }

        System.Random seededRandom = new System.Random(seed);
        int picked = pool[seededRandom.Next(0, pool.Count)];

        Debug.Log($"[MonsterDatabase] Act {act} 몬스터 풀({pool.Count}마리) 중 선택: {picked}");
        return picked;
    }

    // 서버에서 몬스터 스탯 목록을 받아 저장하고, 서버에 없는 id는 로컬 기본값으로 채운다
    public IEnumerator LoadServerDataFromJson()
    {
        string monsterUrl = ApiConfig.Url("/api/game/load-monsters");

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
                Debug.Log($"[서버 스탯 캐싱 완료] ID: {serverData.id}, 이름: {serverData.name}, HP: {serverData.hp}, Shield: {serverData.shield}, Attack: {serverData.attacks}, Defend: {serverData.defend}");
            }

            // 서버에 없는 id는 로컬 기본값으로 보충
            foreach (var local in localMonsters)
            {
                if (!serverStatDict.ContainsKey(local.id))
                {
                    serverStatDict[local.id] = local;
                    Debug.Log($"[로컬 스탯 보충] ID: {local.id}, 이름: {local.name}, HP: {local.hp}, Shield: {local.shield}, Attack: {local.attacks}, Defend: {local.defend}");
                }
            }
        }));
    }

    // 서버 GET 요청 → JSON을 T로 변환해서 onSuccess로 넘김
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

            Debug.Log($"[서버 원본 응답] {request.downloadHandler.text}");

            string jsonString = request.downloadHandler.text;

            T data = JsonUtility.FromJson<T>(jsonString);

            onSuccess?.Invoke(data);
        }
    }
}