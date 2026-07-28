using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Node Prefabs")]
    public GameObject monsterPrefab;
    public GameObject elitePrefab;
    public GameObject bossPrefab;

    [Header("Node Parent")]
    public Transform nodeParent;

    [Header("Map Settings")]
    public int floorCount = 5;
    public float floorSpacing = 180f;
    public float nodeSpacing = 180f;

    private MapSeedGenerator seedGenerator;

    void Start()
    {
        // 같은 오브젝트에 붙어있는 MapSeedGenerator 가져오기
        seedGenerator = GetComponent<MapSeedGenerator>();

        if (seedGenerator != null)
        {
            Random.InitState(seedGenerator.seed);
        }

        GenerateLayout();
    }

    void GenerateLayout()
    {
        // 기존 노드 삭제
        foreach (Transform child in nodeParent)
        {
            Destroy(child.gameObject);
        }

        for (int floor = 0; floor < floorCount; floor++)
        {
            // 마지막 층은 Boss
            if (floor == floorCount - 1)
            {
                CreateNode(bossPrefab, new Vector2(0, floor * floorSpacing - 300f));
                continue;
            }

            // 층마다 2~3개 노드
            int nodeCount = Random.Range(2, 4);

            for (int i = 0; i < nodeCount; i++)
            {
                float x = (i - (nodeCount - 1) / 2f) * nodeSpacing;
                float y = floor * floorSpacing - 300f;

                GameObject prefab = monsterPrefab;

                // 3층부터 25% 확률 Elite
                if (floor >= 2 && Random.value < 0.25f)
                {
                    prefab = elitePrefab;
                }

                CreateNode(prefab, new Vector2(x, y));
            }
        }
    }

    void CreateNode(GameObject prefab, Vector2 pos)
    {
        GameObject node = Instantiate(prefab, nodeParent);

        RectTransform rt = node.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
    }
}