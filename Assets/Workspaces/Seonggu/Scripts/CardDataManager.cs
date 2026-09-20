using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

public class CardDataManager : MonoBehaviour
{
    public static CardDataManager Instance { get; private set; }
    public bool isDataLoaded { get; private set; } = false; // 상태 변수
    public event Action OnDataLoaded; // 이벤트

    public Dictionary<int, Adjective> adjectiveTable = new Dictionary<int, Adjective>();
    public Dictionary<int, Gerund> gerundTable = new Dictionary<int, Gerund>();

    public List<int> defaultAdjectiveIds = new List<int>();
    public List<int> defaultGerundIds = new List<int>();

    public List<ICard> masterCardPool = new List<ICard>(); // 동명사, 형용사 통합 카드 풀

    // 씬을 바로 시작해도 매니저가 알아서 튀어나오게 하는 런타임 초기화 메서드
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        // Resources 폴더에 있는 DataManager 프리팹을 로드해서 동적 생성
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

    public IEnumerator LoadCards()
    {
        // 1. 가져와야 할 데이터들의 URL (서버 API 주소에 맞게 수정해)
        string adjUrl = "http://localhost:8080/api/game/load-adjectives";
        string gerUrl = "http://localhost:8080/api/game/load-gerunds";

        // 2. 동시 처리를 위해 코루틴을 각각 실행하거나, 
        // 여기선 각각 호출하는 것으로 가정할게.
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

    // 중복 코드를 줄이기 위한 제네릭 Fetch 함수
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

    public IEnumerator LoadDefaultDeck()
    {
        string defaultDeckUrl = "http://localhost:8080/api/game/load-default-deck"; // 예시 API

        yield return StartCoroutine(FetchData<DefaultDeckData>(defaultDeckUrl, (deckData) => {
            defaultAdjectiveIds = deckData.adjectiveIds;
            defaultGerundIds = deckData.gerundIds;
        }));
    }

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
