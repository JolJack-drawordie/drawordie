using System.Collections.Generic;
using UnityEngine;

public class BattleFactory : MonoBehaviour
{
    public static BattleFactory Instance;

    //노드 시드 기반 몬스터 생성 시드
    int monsterSpawnSeed;

    [Header("프리팹들")]
    public GameObject playerPrefab;

    // [Act 대응] Act마다 다른 보스를 쓸 수 있도록 act 번호로 묶어서 관리
    [System.Serializable]
    public class BossInfo
    {
        public int act;              // 이 보스가 등장할 Act (1, 2, 3 ...)
        public GameObject bossPrefab;
        public int bossId;           // 서버 Monster.json에 등록된 보스 id (스탯을 여기서 가져옴)
    }

    [Header("보스 (Boss 노드에서만 등장, Act마다 다른 보스를 리스트에 추가)")]
    public List<BossInfo> bossList = new List<BossInfo>();

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

    // 현재 Act에 맞는 보스 정보를 찾아줌 (없으면 null)
    private BossInfo GetBossInfo(int act)
    {
        return bossList.Find(b => b.act == act);
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
        // [보스 분기] 보스 노드면 현재 Act에 맞는 보스를 찾아서 스폰
        bool isBossNode = GameFlowData.currentNodeType == MapNode.NodeType.Boss;
        BossInfo bossInfo = isBossNode ? GetBossInfo(GameFlowData.currentAct) : null;
        bool isBoss = bossInfo != null && bossInfo.bossPrefab != null;

        if (isBossNode && bossInfo == null)
        {
            Debug.LogWarning($"[BattleFactory] Act {GameFlowData.currentAct}에 등록된 보스가 없습니다. bossList에 추가해주세요.");
        }

        int selectedId = -1;
        GameObject enemyPrefab;

        if (isBoss)
        {
            enemyPrefab = bossInfo.bossPrefab;
            selectedId = bossInfo.bossId; // 보스도 몬스터와 동일하게 서버 id로 스탯 조회
            Debug.Log($"보스 노드 → Act {bossInfo.act} 보스 스폰 ({bossInfo.bossPrefab.name}, id={selectedId})");
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

        // 런타임 스탯 주입 (DI) - 보스도 몬스터와 동일하게 서버 Monster.json에서 id로 조회
        UnitBase enemyUnit = enemyObj.GetComponent<UnitBase>();
        if (enemyUnit != null && StatManager.Instance != null)
        {
            if (MonsterDatabase.Instance.TryInitializeMonsterStat(selectedId))
            {
                enemyUnit.Initialize(StatManager.Instance.GetEnemyStat());
            }
            else
            {
                Debug.Log($"{(isBoss ? "보스" : "몬스터")} 스탯을 가져오지 못했습니다. (id={selectedId}) 서버 Monster.json에 등록됐는지 확인하세요.");
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
            PlayerSpineController playerspineController = playerObj.GetComponent<PlayerSpineController>();
            if (playerspineController != null)
            {
                playerspineController.attackTarget = enemyObj.transform;
                Debug.Log("팩토리가 플레이어에게 적 타겟을 성공적으로 연결했습니다.");
            }
        }

        return (playerObj, enemyObj);
    }
}