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

    void Start()
    {
        seedGenerator = GetComponent<MapSeedGenerator>();

        if (seedGenerator != null)
        {
            Random.InitState(seedGenerator.seed);
        }

        GenerateLayout();
    }

    void GenerateLayout()
    {
        foreach (Transform child in nodeParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in lineParent)
        {
            Destroy(child.gameObject);
        }

        List<GameObject> previousFloor = new List<GameObject>();

        for (int floor = 0; floor < floorCount; floor++)
        {
            List<GameObject> currentFloor = new List<GameObject>();

            // 마지막 층 = Boss
            if (floor == floorCount - 1)
            {
                GameObject boss =
                    CreateNode(
                        bossPrefab,
                        new Vector2(0, floor * floorSpacing - 300f),
                        MapNode.NodeType.Boss,
                        floor,
                        0);

                currentFloor.Add(boss);
            }
            else
            {
                int nodeCount = Random.Range(2, 4);

                for (int i = 0; i < nodeCount; i++)
                {
                    float x =
                        (i - (nodeCount - 1) / 2f) * nodeSpacing;

                    float y =
                        floor * floorSpacing - 300f;

                    GameObject prefab = monsterPrefab;
                    MapNode.NodeType type = MapNode.NodeType.Monster;

                    if (floor >= 2 && Random.value < 0.25f)
                    {
                        prefab = elitePrefab;
                        type = MapNode.NodeType.Elite;
                    }

                    GameObject node =
                        CreateNode(
                            prefab,
                            new Vector2(x, y),
                            type,
                            floor,
                            i);

                    currentFloor.Add(node);
                }
            }

            // 이전 층과 현재 층 연결
            if (previousFloor.Count > 0)
            {
                foreach (GameObject prev in previousFloor)
                {
                    MapNode prevNode = prev.GetComponent<MapNode>();

                    foreach (GameObject current in currentFloor)
                    {
                        MapNode currentNode = current.GetComponent<MapNode>();

                        // 연결 정보 저장
                        prevNode.AddConnection(currentNode);

                        // 선 생성
                        CreateLine(
                            prev.GetComponent<RectTransform>(),
                            current.GetComponent<RectTransform>());
                    }
                }
            }

            previousFloor = currentFloor;
        }
    }

    GameObject CreateNode(
        GameObject prefab,
        Vector2 pos,
        MapNode.NodeType type,
        int floor,
        int index)
    {
        GameObject node =
            Instantiate(prefab, nodeParent);

        RectTransform rt =
            node.GetComponent<RectTransform>();

        rt.anchoredPosition = pos;

        // ★ 노드 정보 저장
        MapNode mapNode = node.GetComponent<MapNode>();

        if (mapNode != null)
        {
            mapNode.Initialize(type, floor, index);
        }

        return node;
    }

    void CreateLine(RectTransform from, RectTransform to)
    {
        GameObject line =
            Instantiate(linePrefab, lineParent);

        RectTransform lineRect =
            line.GetComponent<RectTransform>();

        Vector2 start = from.anchoredPosition;
        Vector2 end = to.anchoredPosition;

        Vector2 direction = end - start;

        float distance = direction.magnitude;

        lineRect.anchoredPosition =
            (start + end) / 2f;

        lineRect.sizeDelta =
            new Vector2(distance, 6f);

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        lineRect.localRotation =
            Quaternion.Euler(0, 0, angle);
    }
}