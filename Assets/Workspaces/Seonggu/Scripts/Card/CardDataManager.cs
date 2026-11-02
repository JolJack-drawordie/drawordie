using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

// 서버에서 카드 데이터(형용사 / 동명사 목록, 새 게임 기본 덱)를 받아 보관한다. 첫 씬 로드 전에 자동으로 만들어지고 씬이 바뀌어도 유지된다.
public class CardDataManager : MonoBehaviour
{
    public static CardDataManager Instance { get; private set; }
    public bool isDataLoaded { get; private set; } = false; // 카드 데이터를 다 받았는지
    public event Action OnDataLoaded; // 다 받았을 때 알림 (TurnManager가 전투 시작에 사용)

    // 서버 id로 찾는 카드 데이터 표
    public Dictionary<int, Adjective> adjectiveTable = new Dictionary<int, Adjective>();
    public Dictionary<int, Gerund> gerundTable = new Dictionary<int, Gerund>();

    // 새 게임 시작 덱 (서버가 정함)
    public List<int> defaultAdjectiveIds = new List<int>();
    public List<int> defaultGerundIds = new List<int>();

    public List<ICard> masterCardPool = new List<ICard>(); // 형용사 + 동명사 전체 카드 (보상 후보 등에 사용)

    // 씬에 미리 두지 않아도 Resources의 CardDataManager 프리팹을 게임 시작 시 자동으로 만든다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        GameObject prefab = Resources.Load<GameObject>("CardDataManager");
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab);
            DontDestroyOnLoad(obj);
            Debug.Log("런타임 시점에 DataManager가 성공적으로 생성되었습니다.");
        }
        else
        {
            Debug.LogWarning("Resources 폴더에 DataManager 프리팹이 없습니다! 에디터 테스트 시 씬에 직접 배치해 주세요.");
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        StartCoroutine(LoadCards());
        StartCoroutine(LoadDefaultDeck());
    }

    // 형용사 / 동명사 목록을 차례로 받아 표를 만든 뒤 완료를 알린다
    public IEnumerator LoadCards()
    {
        string adjUrl = ApiConfig.Url("/api/game/load-adjectives");
        string gerUrl = ApiConfig.Url("/api/game/load-gerunds");

        yield return StartCoroutine(FetchData<AdjectiveList>(adjUrl, (list) => {
            adjectiveTable.Clear();
            foreach (var adj in list.adjectives)
                adjectiveTable[adj.id] = adj;
        }));

        yield return StartCoroutine(FetchData<GerundList>(gerUrl, (list) => {
            gerundTable.Clear();
            foreach (var ger in list.gerunds)
                gerundTable[ger.id] = ger;
        }));

        BuildMasterCardPool();

        isDataLoaded = true;
        OnDataLoaded?.Invoke();
        Debug.Log("모든 카드 데이터 로딩 및 딕셔너리 구축 완료!");
    }

    // 서버 GET 요청 → JSON을 T로 변환해서 onComplete로 넘김 (실패하면 로그만)
    private IEnumerator FetchData<T>(string url, System.Action<T> onComplete)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                T data = JsonUtility.FromJson<T>(www.downloadHandler.text);
                onComplete?.Invoke(data);
            }
            else
            {
                Debug.LogError($"{url} 데이터 로딩 실패: {www.error}");
            }
        }
    }

    // 새 게임 기본 덱 받기
    public IEnumerator LoadDefaultDeck()
    {
        string defaultDeckUrl = ApiConfig.Url("/api/game/load-default-deck");

        yield return StartCoroutine(FetchData<DefaultDeckData>(defaultDeckUrl, (deckData) => {
            defaultAdjectiveIds = deckData.adjectiveIds;
            defaultGerundIds = deckData.gerundIds;
        }));
    }

    // 전체 카드 목록을 다시 만든다
    public void BuildMasterCardPool()
    {
        masterCardPool.Clear();

        foreach (var adj in adjectiveTable.Values)
        {
            masterCardPool.Add(new AdjectiveCard(adj));
        }
        foreach (var ger in gerundTable.Values)
        {
            masterCardPool.Add(new GerundCard(ger));
        }
    }
}
