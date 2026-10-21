using UnityEngine;

public class StatManager : MonoBehaviour
{
    public static StatManager Instance { get; private set; }

    [Header("원본 스탯 데이터 (ScriptableObject)")]
    [SerializeField] private UnitStatData originalPlayerStat;
    [SerializeField] private UnitStatData originalEnemyStat;

    [Header("런타임 스탯 데이터 (게임 중 변경되는 데이터)")]
    public UnitStatData runtimePlayerStat { get; private set; }
    public UnitStatData runtimeEnemyStat { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        // 프리팹이 Resources 폴더 안에 "StatManager" 이름으로 있다고 가정
        GameObject prefab = Resources.Load<GameObject>("StatManager");
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);
        }
        else
        {
            // 프리팹이 없다면 새 오브젝트를 생성해서 사용
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

        // 게임 시작 시 매니저 초기화 단계에 런타임 복사본 생성
        InitializeRuntimeStats();
    }

    private void InitializeRuntimeStats()
    {
        if (originalPlayerStat != null)
        {
            // 원본 SO를 직접 건드리지 않도록 인스턴스 생성(Instantiate) 사용
            runtimePlayerStat = Instantiate(originalPlayerStat);
            runtimePlayerStat.ResetStat();
        }
    }

    // BattleFactory에서 호출할 유저 스탯 반환
    public UnitStatData GetPlayerStat()
    {
        // 혹시 초기화가 안 되었거나 이전 세션의 체력이 0인 경우 예외 처리 추가 가능
        return runtimePlayerStat;
    }

    public UnitStatData GetEnemyStat()
    {
        return runtimeEnemyStat;
    }

    public void SetEnemyStat(int hp, int shield)
    {
        if (runtimeEnemyStat == null)
        {
            // ScriptableObject는 반드시 CreateInstance로 생성해야 합니다
            runtimeEnemyStat = ScriptableObject.CreateInstance<UnitStatData>();
        }

        runtimeEnemyStat.maxHp = hp;
        runtimeEnemyStat.currentHp = hp;
        runtimeEnemyStat.maxShield = shield;
        runtimeEnemyStat.currentShield = 0;
    }

    // 전투 종료 시 플레이어 상태를 저장하거나 갱신할 때
    public void SavePlayerState(PlayerUnit player)
    {
        if (runtimePlayerStat != null && player != null)
        {
            runtimePlayerStat.currentHp = player.statData.currentHp;
            // 필요한 추가 데이터 동기화
        }
    }

    // 지정 수치 체력 회복
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

    // 최대 체력 비율 퍼센트 회복
    public void HealPlayerPercent(float ratio)
    {
        if (runtimePlayerStat == null) return;

        int healAmount = Mathf.RoundToInt(runtimePlayerStat.maxHp * ratio);
        HealPlayer(healAmount);
    }

    // 새 게임: 유물로 늘어난 최대 HP를 원본 값으로 되돌림
    public void ResetPlayerMaxHp()
    {
        if (runtimePlayerStat == null || originalPlayerStat == null) return;

        runtimePlayerStat.maxHp = originalPlayerStat.maxHp;
    }

    // 불러오기: 저장된 최대 HP 복원 (구버전 세이브처럼 값이 없으면 원본 값)
    public void SetPlayerMaxHp(int maxHp)
    {
        if (runtimePlayerStat == null) return;

        if (maxHp > 0)
            runtimePlayerStat.maxHp = maxHp;
        else
            ResetPlayerMaxHp();
    }

    // 유물 효과: 플레이어 최대 HP 증가
    public void IncreasePlayerMaxHp(int amount)
    {
        if (runtimePlayerStat == null)
        {
            Debug.LogError(
                "[StatManager] 플레이어 스탯이 초기화되지 않았습니다."
            );
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning(
                "[StatManager] 증가할 최대 HP가 0 이하입니다."
            );
            return;
        }

        runtimePlayerStat.maxHp += amount;

        Debug.Log(
            $"[StatManager] 최대 HP 증가 (+{amount}) -> " +
            $"{runtimePlayerStat.currentHp}/" +
            $"{runtimePlayerStat.maxHp}"
        );
    }
}