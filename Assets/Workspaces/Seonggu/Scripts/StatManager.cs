using UnityEngine;

public class StatManager : MonoBehaviour
{
    public static StatManager Instance { get; private set; }

    [Header("원본 스탯 데이터 (ScriptableObject)")]
    [SerializeField] private UnitStatData originalPlayerStat;
    [SerializeField] private UnitStatData originalEnemyStat;

    [Header("런타임 전용 스탯 (전투 중 변동되는 데이터)")]
    public UnitStatData runtimePlayerStat { get; private set; }
    public UnitStatData runtimeEnemyStat { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        // 프로젝트 폴더의 Resources 폴더 안에 "StatManager" 프리팹이 있다고 가정
        GameObject prefab = Resources.Load<GameObject>("StatManager");
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);
        }
        else
        {
            // 프리팹이 없다면 빈 오브젝트를 동적으로 생성해서 부착
            GameObject obj = new GameObject("StatManager");
            obj.AddComponent<StatManager>();
            DontDestroyOnLoad(obj);
        }
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
            return;
        }

        // 게임 시작 시점 또는 매니저 초기화 시점에 런타임용 복사본 생성
        InitializeRuntimeStats();
    }

    private void InitializeRuntimeStats()
    {
        if (originalPlayerStat != null)
        {
            // 원본 SO를 직접 건드리지 않도록 인스턴스 복제(Instantiate) 사용
            runtimePlayerStat = Instantiate(originalPlayerStat);
            runtimePlayerStat.ResetStat();
        }

        if (originalEnemyStat != null)
        {
            runtimeEnemyStat = Instantiate(originalEnemyStat);
            runtimeEnemyStat.ResetStat();
        }
    }

    // BattleFactory가 호출할 스탯 제공 메서드
    public UnitStatData GetPlayerStat()
    {
        // 혹시 초기화가 안 되었거나 전투 재시작 시 체력이 0일 경우 대비해 리셋 로직 추가 가능
        return runtimePlayerStat;
    }

    public UnitStatData GetEnemyStat()
    {
        return runtimeEnemyStat;
    }

    // 전투 종료 후 플레이어 상태를 저장하거나 갱신할 때 사용
    public void SavePlayerState(PlayerUnit player)
    {
        if (runtimePlayerStat != null && player != null)
        {
            runtimePlayerStat.currentHp = player.statData.currentHp;
            // 필요시 추가 데이터 동기화
        }
    }

    // 고정 수치 체력 회복
    public void HealPlayer(int amount)
    {
        if (runtimePlayerStat == null) return;

        runtimePlayerStat.currentHp += amount;

        // 최대 체력을 초과하지 않도록 제한
        if (runtimePlayerStat.currentHp > runtimePlayerStat.maxHp)
        {
            runtimePlayerStat.currentHp = runtimePlayerStat.maxHp;
        }

        Debug.Log($"[StatManager] 플레이어 체력 회복 (+{amount}) -> 현재 체력: {runtimePlayerStat.currentHp}/{runtimePlayerStat.maxHp}");
    }

    // 최대 체력 대비 비율 회복
    public void HealPlayerPercent(float ratio)
    {
        if (runtimePlayerStat == null) return;

        int healAmount = Mathf.RoundToInt(runtimePlayerStat.maxHp * ratio);
        HealPlayer(healAmount);
    }
}