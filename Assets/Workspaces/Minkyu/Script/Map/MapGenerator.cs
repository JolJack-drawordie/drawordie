using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Node Prefabs")]
    public GameObject monsterPrefab;
    public GameObject elitePrefab;
    public GameObject bossPrefab;

    [Header("Parents")]
    public Transform nodeParent;
    public Transform lineParent;

    [Header("Line")]
    public GameObject linePrefab;

    [Header("Map Settings")]
    public int floorCount = 5;
    public float floorSpacing = 180f;
    public float nodeSpacing = 180f;

    private MapSeedGenerator seedGenerator;

    // 층별 노드 저장
    private List<List<GameObject>> floorNodes = new List<List<GameObject>>();

    void Start()
    {
        seedGenerator = GetComponent<MapSeedGenerator>();

        if (seedGenerator != null)
        {
            Random.InitState(seedGenerator.seed);
        }

        GenerateLayout();
        GenerateConnections();
    }

    void GenerateLayout()
    {
        foreach (Transform child in nodeParent)
            Destroy(child.gameObject);

        foreach (Transform child in lineParent)
            Destroy(child.gameObject);

        floorNodes.Clear();

        for (int floor = 0; floor < floorCount; floor++)
        {
            List<GameObject> currentFloor = new List<GameObject>();

            if (floor == floorCount - 1)
            {
                GameObject boss = CreateNode(
                    bossPrefab,
                    new Vector2(0, floor * floorSpacing - 300f));

                currentFloor.Add(boss);
            }
            else
            {
                int nodeCount = Random.Range(2, 4);

                for (int i = 0; i < nodeCount; i++)
                {
                    float x = (i - (nodeCount - 1) / 2f) * nodeSpacing;
                    float y = floor * floorSpacing - 300f;

                    GameObject prefab = monsterPrefab;

                    if (floor >= 2 && Random.value < 0.25f)
                        prefab = elitePrefab;

                    currentFloor.Add(
                        CreateNode(prefab, new Vector2(x, y))
                    );
                }
            }

            floorNodes.Add(currentFloor);
        }
    }

    void GenerateConnections()
    {
        for (int floor = 0; floor < floorNodes.Count - 1; floor++)
        {
            List<GameObject> currentFloor = floorNodes[floor];
            List<GameObject> nextFloor = floorNodes[floor + 1];

            foreach (GameObject currentNode in currentFloor)
            {
                // 최소 1개, 최대 2개 연결
                int connectionCount = Random.Range(1, 3);

                List<int> connectedIndex = new List<int>();

                for (int i = 0; i < connectionCount; i++)
                {
                    int randomIndex = Random.Range(0, nextFloor.Count);

                    if (connectedIndex.Contains(randomIndex))
                        continue;

                    connectedIndex.Add(randomIndex);

                    GameObject nextNode = nextFloor[randomIndex];

                    CreateLine(
                        currentNode.GetComponent<RectTransform>(),
                        nextNode.GetComponent<RectTransform>());
                }
            }
        }
    }

    GameObject CreateNode(GameObject prefab, Vector2 pos)
    {
        GameObject node = Instantiate(prefab, nodeParent);

        RectTransform rt = node.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;

        return node;
    }

    void CreateLine(RectTransform from, RectTransform to)
    {
        GameObject line = Instantiate(linePrefab, lineParent);

        RectTransform lineRect = line.GetComponent<RectTransform>();

        Vector2 start = from.anchoredPosition;
        Vector2 end = to.anchoredPosition;

        Vector2 direction = end - start;

        lineRect.anchoredPosition = (start + end) / 2f;

        lineRect.sizeDelta = new Vector2(direction.magnitude, 6f);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}