using System.Collections.Generic;
using UnityEngine;

// 전투 시작 시 플레이어와 적을 스폰 위치에 만들고 스탯을 넣어 준다.
// 적은 노드 종류로 정한다: 보스 노드 → Act별 보스, 중간보스 노드 → Act별 중간보스, 일반 노드 → 노드 시드로 고른 몬스터.
public class BattleFactory : MonoBehaviour
{
    public static BattleFactory Instance;

    [Header("프리팹들")]
    public GameObject playerPrefab;

    // Act별 보스 / 중간보스 (프리팹 + 서버 Monster.json id)
    [System.Serializable]
    public class BossInfo
    {
        public int act; // 등장할 Act (1, 2, 3)
        public GameObject bossPrefab;
        public int bossId; // 서버 Monster.json의 id (스탯을 여기서 가져옴)
    }

    [Header("보스 (Boss 노드에서만 등장, Act마다 다른 보스를 리스트에 추가)")]
    public List<BossInfo> bossList = new List<BossInfo>();

    [Header("중간보스 (Elite 노드에서만 등장, Act마다 하나씩. 서버 id는 x40 권장: 140, 240, 340)")]
    public List<BossInfo> eliteList = new List<BossInfo>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // 이 Act의 보스 정보 (없으면 null)
    private BossInfo GetBossInfo(int act)
    {
        return bossList.Find(b => b.act == act);
    }

    // 이 Act의 중간보스 정보 (없으면 null)
    private BossInfo GetEliteInfo(int act)
    {
        return eliteList.Find(b => b.act == act);
    }

    // 플레이어 생성 + 씬 사이에서 유지되는 플레이어 스탯 연결
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
        playerObj.SetActive(false); // 스탯을 넣을 때까지 꺼 둠 (Awake / Start가 스탯 없이 돌지 않도록)

        UnitBase playerUnit = playerObj.GetComponent<UnitBase>();
        if (playerUnit != null && StatManager.Instance != null)
        {
            playerUnit.Initialize(StatManager.Instance.GetPlayerStat());
        }

        // 체력 / 방어도 바 연결
        if (UIManager.Instance != null)
        {
            UIManager.Instance.LinkUnitToUI(playerUnit);
        }

        playerObj.SetActive(true);

        Debug.Log("팩토리가 플레이어 유닛을 생성했습니다.");
        return playerObj;
    }

    // 적 생성: 노드 종류에 맞는 프리팹을 고르고 서버 스탯을 넣은 뒤 켠다
    public GameObject SpawnEnemy(GameObject spawnPoint)
    {
        // 보스 노드: 이 Act의 보스
        bool isBossNode = GameFlowData.currentNodeType == MapNode.NodeType.Boss;
        BossInfo bossInfo = isBossNode ? GetBossInfo(GameFlowData.currentAct) : null;
        bool isBoss = bossInfo != null && bossInfo.bossPrefab != null;

        if (isBossNode && bossInfo == null)
        {
            Debug.LogWarning($"[BattleFactory] Act {GameFlowData.currentAct}에 등록된 보스가 없습니다. bossList에 추가해주세요.");
        }

        // 중간보스 노드: 이 Act의 중간보스 (없으면 일반 몬스터로 대신함)
        bool isEliteNode = GameFlowData.currentNodeType == MapNode.NodeType.Elite;
        BossInfo eliteInfo = isEliteNode ? GetEliteInfo(GameFlowData.currentAct) : null;
        bool isElite = eliteInfo != null && eliteInfo.bossPrefab != null;

        if (isEliteNode && !isElite)
        {
            Debug.LogWarning($"[BattleFactory] Act {GameFlowData.currentAct}에 등록된 중간보스가 없어 일반 몬스터로 대체합니다. eliteList에 추가해주세요.");
        }

        int selectedId = -1;
        GameObject enemyPrefab;

        if (isBoss)
        {
            enemyPrefab = bossInfo.bossPrefab;
            selectedId = bossInfo.bossId; // 보스도 일반 몬스터처럼 서버 id로 스탯을 가져옴
            Debug.Log($"보스 노드 → Act {bossInfo.act} 보스 스폰 ({bossInfo.bossPrefab.name}, id={selectedId})");
        }
        else if (isElite)
        {
            enemyPrefab = eliteInfo.bossPrefab;
            selectedId = eliteInfo.bossId;
            Debug.Log($"중간보스 노드 → Act {eliteInfo.act} 중간보스 스폰 ({eliteInfo.bossPrefab.name}, id={selectedId})");
        }
        else
        {
            // 일반 몬스터: 노드 시드로 고름 (같은 노드면 불러와도 같은 몬스터). 스폰 시점에 계산해야 시드가 맞음
            int nodeSeed = GameFlowData.currentNodeSeed;
            System.Random seedGenerator = new System.Random(nodeSeed + 1);
            int seed = seedGenerator.Next(); // 몬스터 선택용 시드
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
        enemyObj.SetActive(false); // 스탯을 넣을 때까지 꺼 둠

        // 서버 Monster.json 스탯을 id로 찾아 넣는다
        UnitBase enemyUnit = enemyObj.GetComponent<UnitBase>();
        if (enemyUnit != null && StatManager.Instance != null)
        {
            if (MonsterDatabase.Instance.TryInitializeMonsterStat(selectedId))
            {
                enemyUnit.Initialize(StatManager.Instance.GetEnemyStat());
            }
            else
            {
                Debug.Log($"{(isBoss ? "보스" : isElite ? "중간보스" : "몬스터")} 스탯을 가져오지 못했습니다. (id={selectedId}) 서버 Monster.json에 등록됐는지 확인하세요.");
            }
        }

        // 체력 / 방어도 바 연결
        if (UIManager.Instance != null)
        {
            UIManager.Instance.LinkUnitToUI(enemyUnit);
        }

        enemyObj.SetActive(true); // 스탯을 넣은 뒤 켜서 Awake / Start가 정상적으로 동작하게

        Debug.Log("팩토리가 적 유닛을 생성했습니다.");
        return enemyObj;
    }

    // 플레이어와 적을 만들고, 플레이어의 공격 대상으로 적을 연결
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