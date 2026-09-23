using UnityEngine;

public class BattleFactory : MonoBehaviour
{
    public static BattleFactory Instance;
    
    int monsterSpawnSeed;

    [Header("프리팹들")]
    public GameObject playerPrefab;

    [Header("보스 (Boss 노드에서만 등장)")]
    public GameObject bossPrefab;
    public int bossHp = 100;
    public int bossMaxShield = 30;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    { 
        // 현재 노드의 시드를 바탕으로 몬스터 시드 생성
        int nodeSeed = GameFlowData.currentNodeSeed;
        System.Random seedGenerator = new System.Random(nodeSeed + 1);

        monsterSpawnSeed = seedGenerator.Next();
    }

    public GameObject SpawnPlayer(GameObject spawnPoint)
    {
        if (playerPrefab == null)
        {
            Debug.LogWarning("플레이어 프리팹이 지정되지 않았습니다.");
            return null;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.transform.rotation : Quaternion.identity;

        GameObject playerObj = Instantiate(playerPrefab, pos, rot);
        playerObj.SetActive(false); // 데이터가 주입될 때까지 숨김

        // 런타임 스탯 주입 (DI)
        UnitBase playerUnit = playerObj.GetComponent<UnitBase>();
        if (playerUnit != null && StatManager.Instance != null)
        {
            playerUnit.Initialize(StatManager.Instance.GetPlayerStat());
        }

        // 팩토리가 직접 UI 매니저에 링크 (유닛이 스스로 하던 걸 여기서 안전하게 처리)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.LinkUnitToUI(playerUnit);
        }

        playerObj.SetActive(true);

        Debug.Log("팩토리가 플레이어 유닛을 생성했습니다.");
        return playerObj;
    }

    public GameObject SpawnEnemy(GameObject spawnPoint)
    {
        // [보스 분기] 보스 노드이고 보스 프리팹이 지정되어 있으면 보스를 스폰
        Debug.Log($"[BattleFactory] nodeType={GameFlowData.currentNodeType}, bossPrefab={(bossPrefab != null)}");
        bool isBoss = GameFlowData.currentNodeType == MapNode.NodeType.Boss && bossPrefab != null;

        int selectedId = -1;
        GameObject enemyPrefab;

        if (isBoss)
        {
            enemyPrefab = bossPrefab;
            Debug.Log("보스 노드 → 보스 스폰");
        }
        else
        {
            int seed = monsterSpawnSeed; // 몬스터 시드값
            Debug.Log("몬스터 시드 : " + seed);
            selectedId = MonsterDatabase.Instance.GetRandomMonsterId(seed);
            enemyPrefab = MonsterDatabase.Instance.GetPrefab((MonsterType)selectedId);
        }

        if (enemyPrefab == null)
        {
            Debug.LogWarning("적 프리팹이 지정되지 않았습니다.");
            return null;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.transform.rotation : Quaternion.identity;

        GameObject enemyObj = Instantiate(enemyPrefab, pos, rot);
        enemyObj.SetActive(false); // 데이터가 주입될 때까지 숨김

        // 런타임 스탯 주입 (DI)
        UnitBase enemyUnit = enemyObj.GetComponent<UnitBase>();
        if (enemyUnit != null && StatManager.Instance != null)
        {
            if (isBoss)
            {
                // 보스는 서버 데이터 대신 인스펙터 값으로 스탯 주입
                StatManager.Instance.SetEnemyStat(bossHp, bossMaxShield);
                enemyUnit.Initialize(StatManager.Instance.GetEnemyStat());
            }
            else if (MonsterDatabase.Instance.TryInitializeMonsterStat(selectedId))
            {
                enemyUnit.Initialize(StatManager.Instance.GetEnemyStat());
            }
            else
            {
                Debug.Log("몬스터 스탯을 가져오지 못했습니다.");
            }
        }

        // 팩토리가 직접 UI 매니저에 링크 (유닛이 스스로 하던 걸 여기서 안전하게 처리)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.LinkUnitToUI(enemyUnit);
        }

        enemyObj.SetActive(true); // 데이터 주입 완료 후 활성화 (Awake/Start 정상 작동)

        Debug.Log("팩토리가 적 유닛을 생성했습니다.");
        return enemyObj;
    }

    public (GameObject player, GameObject enemy) SpawnAll(GameObject playerSpawnPoint, GameObject enemySpawnPoint)
    {
        GameObject playerObj = SpawnPlayer(playerSpawnPoint);
        GameObject enemyObj = SpawnEnemy(enemySpawnPoint);

        if (playerObj != null && enemyObj != null)
        {
            PlayerController playerController = playerObj.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.attackTarget = enemyObj.transform;
                Debug.Log("팩토리가 플레이어에게 적 타겟을 성공적으로 연결했습니다.");
            }
        }

        return (playerObj, enemyObj);
    }
}