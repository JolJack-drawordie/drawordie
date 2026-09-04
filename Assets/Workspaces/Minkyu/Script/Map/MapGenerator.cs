using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Node Prefabs")]
    public GameObject monsterPrefab;
    public GameObject elitePrefab;
    public GameObject restPrefab;
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

    [Header("Rest Settings")]
    [Range(0f, 1f)]
    public float restChance = 0.2f;

    private MapSeedGenerator seedGenerator;

    private void Awake()
    {
        seedGenerator = GetComponent<MapSeedGenerator>();

        if (seedGenerator != null)
        {
            Random.InitState(seedGenerator.seed);
        }

        // 맵을 먼저 생성
        GenerateLayout();
    }

    void GenerateLayout()
    {
        // 기존 노드 삭제
        foreach (Transform child in nodeParent)
        {
            Destroy(child.gameObject);
        }

        // 기존 선 삭제
        foreach (Transform child in lineParent)
        {
            Destroy(child.gameObject);
        }

        List<GameObject> previousFloor =
            new List<GameObject>();

        for (int floor = 0; floor < floorCount; floor++)
        {
            List<GameObject> currentFloor =
                new List<GameObject>();

            // 마지막 층 = Boss
            if (floor == floorCount - 1)
            {
                GameObject boss =
                    CreateNode(
                        bossPrefab,
                        new Vector2(
                            0,
                            floor * floorSpacing - 300f
                        ),
                        floor,
                        0,
                        MapNode.NodeType.Boss
                    );

                currentFloor.Add(boss);
            }
            else
            {
                // 일반 층은 2~3개의 노드 생성
                int nodeCount =
                    Random.Range(2, 4);

                for (int i = 0; i < nodeCount; i++)
                {
                    float x =
                        (i - (nodeCount - 1) / 2f)
                        * nodeSpacing;

                    float y =
                        floor * floorSpacing - 300f;

                    GameObject prefab =
                        monsterPrefab;

                    MapNode.NodeType nodeType =
                        MapNode.NodeType.Monster;

                    // 2층부터 Rest / Elite 생성
                    if (floor >= 2)
                    {
                        float randomValue =
                            Random.value;

                        // Rest
                        if (randomValue < restChance)
                        {
                            prefab = restPrefab;

                            nodeType =
                                MapNode.NodeType.Rest;
                        }
                        // Elite
                        else if (
                            randomValue <
                            restChance + 0.25f)
                        {
                            prefab = elitePrefab;

                            nodeType =
                                MapNode.NodeType.Elite;
                        }
                    }

                    GameObject node =
                        CreateNode(
                            prefab,
                            new Vector2(x, y),
                            floor,
                            i,
                            nodeType
                        );

                    currentFloor.Add(node);
                }
            }

            // 이전 층과 현재 층 연결
            if (previousFloor.Count > 0)
            {
                foreach (GameObject prev
                    in previousFloor)
                {
                    MapNode prevNode =
                        prev.GetComponent<MapNode>();

                    foreach (GameObject current
                        in currentFloor)
                    {
                        MapNode currentNode =
                            current.GetComponent<MapNode>();

                        // 연결 정보 저장
                        prevNode.AddConnection(
                            currentNode
                        );

                        // 선 생성
                        CreateLine(
                            prev.GetComponent<RectTransform>(),
                            current.GetComponent<RectTransform>()
                        );
                    }
                }
            }

            previousFloor =
                currentFloor;
        }

        Debug.Log(
            "Map Generate Complete"
        );
    }

    GameObject CreateNode(
        GameObject prefab,
        Vector2 pos,
        int floor,
        int index,
        MapNode.NodeType nodeType)
    {
        if (prefab == null)
        {
            Debug.LogError(
                $"Node Prefab이 없습니다. " +
                $"Type : {nodeType}"
            );

            return null;
        }

        GameObject node =
            Instantiate(
                prefab,
                nodeParent
            );

        RectTransform rt =
            node.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchoredPosition =
                pos;
        }

        // MapNode 정보 설정
        MapNode mapNode =
            node.GetComponent<MapNode>();

        if (mapNode != null)
        {
            mapNode.Initialize(
                nodeType,
                floor,
                index
            );
        }

        return node;
    }

    void CreateLine(
        RectTransform from,
        RectTransform to)
    {
        if (from == null || to == null)
            return;

        if (linePrefab == null)
            return;

        GameObject line =
            Instantiate(
                linePrefab,
                lineParent
            );

        RectTransform lineRect =
            line.GetComponent<RectTransform>();

        Vector2 start =
            from.anchoredPosition;

        Vector2 end =
            to.anchoredPosition;

        Vector2 direction =
            end - start;

        float distance =
            direction.magnitude;

        // 선 위치
        lineRect.anchoredPosition =
            (start + end) / 2f;

        // 선 길이
        lineRect.sizeDelta =
            new Vector2(
                distance,
                6f
            );

        // 선 회전
        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        lineRect.localRotation =
            Quaternion.Euler(
                0,
                0,
                angle
            );
    }
}