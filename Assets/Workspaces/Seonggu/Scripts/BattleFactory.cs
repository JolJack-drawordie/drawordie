using UnityEngine;

public class BattleFactory : MonoBehaviour
{
    public static BattleFactory Instance;

    [Header("프리팹들")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
            enemyUnit.Initialize(StatManager.Instance.GetEnemyStat());
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