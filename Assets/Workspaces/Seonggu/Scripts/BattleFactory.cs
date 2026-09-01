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