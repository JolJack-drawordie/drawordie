using UnityEngine;
using System.Collections.Generic;

// 플레이어 / 적 스탯을 관리한다. 첫 씬 로드 전에 자동으로 만들어지고 씬이 바뀌어도 유지되어 플레이어 체력이 다음 전투로 이어진다.
// 원본 스탯 에셋은 건드리지 않고 게임 중에는 복사본(runtime)만 바꾼다.
public class StatManager : MonoBehaviour
{
    public static StatManager Instance { get; private set; }

    [Header("원본 스탯 데이터 (ScriptableObject)")]
    [SerializeField] private UnitStatData originalPlayerStat;

    [Header("런타임 스탯 데이터 (게임 중 변경되는 데이터)")]
    public UnitStatData runtimePlayerStat { get; private set; }
    public UnitStatData runtimeEnemyStat { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        // Resources의 StatManager 프리팹을 만들고, 없으면 빈 오브젝트에 붙여서 만든다
        GameObject prefab = Resources.Load<GameObject>("StatManager");
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);
        }
        else
        {
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

        // 게임 시작 시 플레이어 스탯 복사본 생성
        InitializeRuntimeStats();
    }

    // 원본 에셋을 직접 바꾸지 않도록 복사본을 만들어 쓴다
    private void InitializeRuntimeStats()
    {
        if (originalPlayerStat != null)
        {
            runtimePlayerStat = Instantiate(originalPlayerStat);
            runtimePlayerStat.ResetStat();
        }
    }

    // BattleFactory가 플레이어를 만들 때 넣어 줄 스탯
    public UnitStatData GetPlayerStat()
    {
        return runtimePlayerStat;
    }

    public UnitStatData GetEnemyStat()
    {
        return runtimeEnemyStat;
    }

    // 적 스탯 설정 (전투마다 덮어씀)
    public void SetEnemyStat(int hp, int shield, List<AttackData> attacks, int defend)
    {
        if (runtimeEnemyStat == null)
        {
            // ScriptableObject는 CreateInstance로 만들어야 함
            runtimeEnemyStat = ScriptableObject.CreateInstance<UnitStatData>();
        }

        runtimeEnemyStat.maxHp = hp;
        runtimeEnemyStat.currentHp = hp;
        runtimeEnemyStat.maxShield = shield;
        runtimeEnemyStat.currentShield = 0;
        runtimeEnemyStat.attacks = attacks;
        runtimeEnemyStat.defend = defend;
        runtimeEnemyStat.monsterId = -1;
        runtimeEnemyStat.boss = null;
    }

    // 서버 Monster.json 항목 하나를 통째로 넣는다 (id: 아이콘 / 세이브용, boss: 보스 기믹 수치)
    public void SetEnemyStat(MonsterServerData data)
    {
        SetEnemyStat(data.hp, data.shield, data.attacks, data.defend);
        runtimeEnemyStat.monsterId = data.id;
        runtimeEnemyStat.boss = data.boss;
    }

    // 플레이어 체력 회복 (최대 체력까지, 휴식 씬 등)
    public void HealPlayer(int amount)
    {
        if (runtimePlayerStat == null) return;

        runtimePlayerStat.currentHp += amount;

        if (runtimePlayerStat.currentHp > runtimePlayerStat.maxHp)
        {
            runtimePlayerStat.currentHp = runtimePlayerStat.maxHp;
        }

        Debug.Log($"[StatManager] 플레이어 체력 회복 (+{amount}) -> 현재 체력: {runtimePlayerStat.currentHp}/{runtimePlayerStat.maxHp}");
    }

    // 새 게임: 유물로 늘어난 최대 체력을 원본 값으로 되돌림
    public void ResetPlayerMaxHp()
    {
        if (runtimePlayerStat == null || originalPlayerStat == null) return;

        runtimePlayerStat.maxHp = originalPlayerStat.maxHp;
    }

    // 불러오기: 저장된 최대 체력 복원 (예전 세이브처럼 값이 없으면 원본 값)
    public void SetPlayerMaxHp(int maxHp)
    {
        if (runtimePlayerStat == null) return;

        if (maxHp > 0)
            runtimePlayerStat.maxHp = maxHp;
        else
            ResetPlayerMaxHp();
    }

    // 유물 효과: 플레이어 최대 체력 증가
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