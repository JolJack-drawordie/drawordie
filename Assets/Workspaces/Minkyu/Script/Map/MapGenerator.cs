using System;
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
    public float floorSpacing = 210f;
    public float nodeSpacing = 230f;

    // 1층 노드의 y 위치 (1920x1080 기준). 보스 노드가 상단 바(높이 100)에 가리지 않도록 아래로 내림
    public float startY = -370f;

    [Header("Rest Settings")]
    [Range(0f, 1f)]
    public float restChance = 0.2f;

    [Header("Connection Settings")]
    [Range(1, 2)]
    public int minConnections = 1;

    [Range(1, 2)]
    public int maxConnections = 2;

    // =========================
    // Map Seed 기반 Random
    // =========================

    private System.Random mapRandom;

    private MapSeedGenerator mapSeedGenerator;

    private void Awake()
    {
        // MapSeedGenerator 가져오기
        mapSeedGenerator =
            GetComponent<MapSeedGenerator>();

        if (mapSeedGenerator == null)
        {
            Debug.LogError(
                "MapGenerator : MapSeedGenerator가 없습니다."
            );

            return;
        }

        // Map Seed 확인
        int mapSeed =
            mapSeedGenerator.seed;

        // Map Seed를 기반으로
        // Map 전용 Random 생성
        mapRandom =
            new System.Random(mapSeed);

        Debug.Log(
            "MapGenerator Map Seed : " +
            mapSeed
        );

        // 맵 생성
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

        for (int floor = 0;
            floor < floorCount;
            floor++)
        {
            List<GameObject> currentFloor =
                new List<GameObject>();

            // =========================
            // 마지막 층 = Boss
            // =========================

            if (floor == floorCount - 1)
            {
                GameObject boss =
                    CreateNode(
                        bossPrefab,
                        new Vector2(
                            0,
                            startY + floor * floorSpacing
                        ),
                        floor,
                        0,
                        MapNode.NodeType.Boss
                    );

                currentFloor.Add(boss);
            }
            else
            {
                // =========================
                // 일반 층은 2~3개의 노드 생성
                // =========================

                int nodeCount =
                    mapRandom.Next(3, 5);

                // =========================
                // 보스 직전 층인지 확인
                // =========================

                bool isBossBeforeFloor =
                    floor == floorCount - 2;

                for (int i = 0;
                    i < nodeCount;
                    i++)
                {
                    float x =
                        (i - (nodeCount - 1) / 2f)
                        * nodeSpacing;

                    float y =
                        startY + floor * floorSpacing;

                    GameObject prefab =
                        monsterPrefab;

                    MapNode.NodeType nodeType =
                        MapNode.NodeType.Monster;

                    // =========================
                    // 보스 직전 층
                    // 모든 노드를 Rest로 생성
                    // =========================

                    if (isBossBeforeFloor)
                    {
                        prefab = restPrefab;

                        nodeType =
                            MapNode.NodeType.Rest;

                        Debug.Log(
                            $"보스 직전 휴식 노드 생성 : " +
                            $"Floor {floor} / Index {i}"
                        );
                    }
                    else
                    {
                        // =========================
                        // 일반 층의 Rest / Elite 랜덤 생성
                        // =========================

                        if (floor >= 2)
                        {
                            double randomValue =
                                mapRandom.NextDouble();

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

            // =========================
            // 이전 층과 현재 층 연결
            // =========================

            if (previousFloor.Count > 0)
            {
                CreateConnections(
                    previousFloor,
                    currentFloor
                );
            }

            previousFloor =
                currentFloor;
        }

        Debug.Log(
            "Map Generate Complete"
        );
    }


    /// <summary>
    /// 이전 층과 현재 층을 연결한다.
    /// 가까운 노드를 우선 연결하고, 연결되지 않은 노드는 보정한다.
    /// </summary>
    void CreateConnections(
        List<GameObject> previousFloor,
        List<GameObject> currentFloor)
    {
        if (previousFloor == null || currentFloor == null)
            return;

        if (previousFloor.Count == 0 || currentFloor.Count == 0)
            return;

        // 현재 층 각 노드의 유입 연결 개수
        int[] incomingConnections = new int[currentFloor.Count];

        // 각 이전 층 노드에서 가까운 노드를 우선 연결
        foreach (GameObject prev in previousFloor)
        {
            if (prev == null)
                continue;

            MapNode prevNode = prev.GetComponent<MapNode>();
            RectTransform prevRect = prev.GetComponent<RectTransform>();

            if (prevNode == null || prevRect == null)
                continue;

            // 기존 설정에 따라 연결 개수 결정
            int minCount = Mathf.Clamp(minConnections, 1, currentFloor.Count);
            int maxCount = Mathf.Clamp(
                maxConnections,
                minCount,
                currentFloor.Count
            );

            int connectionCount = mapRandom.Next(
                minCount,
                maxCount + 1
            );

            // 가로 거리가 가까운 순서대로 후보 정렬
            List<int> candidateIndexes = new List<int>();

            for (int i = 0; i < currentFloor.Count; i++)
            {
                if (currentFloor[i] != null &&
                    currentFloor[i].GetComponent<MapNode>() != null &&
                    currentFloor[i].GetComponent<RectTransform>() != null)
                {
                    candidateIndexes.Add(i);
                }
            }

            candidateIndexes.Sort((a, b) =>
            {
                float distanceA = Mathf.Abs(
                    currentFloor[a].GetComponent<RectTransform>()
                        .anchoredPosition.x - prevRect.anchoredPosition.x
                );

                float distanceB = Mathf.Abs(
                    currentFloor[b].GetComponent<RectTransform>()
                        .anchoredPosition.x - prevRect.anchoredPosition.x
                );

                return distanceA.CompareTo(distanceB);
            });

            int actualCount = Mathf.Min(
                connectionCount,
                candidateIndexes.Count
            );

            // 가까운 후보부터 연결
            for (int i = 0; i < actualCount; i++)
            {
                int index = candidateIndexes[i];

                MapNode currentNode =
                    currentFloor[index].GetComponent<MapNode>();

                prevNode.AddConnection(currentNode);
                incomingConnections[index]++;

                CreateLine(
                    prevRect,
                    currentFloor[index].GetComponent<RectTransform>()
                );
            }
        }

        // 유입 연결이 없는 노드는 이전 층의 가장 가까운 노드와 연결
        for (int i = 0; i < currentFloor.Count; i++)
        {
            if (incomingConnections[i] > 0 || currentFloor[i] == null)
                continue;

            MapNode currentNode =
                currentFloor[i].GetComponent<MapNode>();

            RectTransform currentRect =
                currentFloor[i].GetComponent<RectTransform>();

            if (currentNode == null || currentRect == null)
                continue;

            GameObject closestPrevious = null;
            MapNode closestPreviousNode = null;
            RectTransform closestPreviousRect = null;
            float shortestDistance = float.MaxValue;

            foreach (GameObject prev in previousFloor)
            {
                if (prev == null)
                    continue;

                MapNode prevNode = prev.GetComponent<MapNode>();
                RectTransform prevRect = prev.GetComponent<RectTransform>();

                if (prevNode == null || prevRect == null)
                    continue;

                float distance = Mathf.Abs(
                    prevRect.anchoredPosition.x -
                    currentRect.anchoredPosition.x
                );

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPrevious = prev;
                    closestPreviousNode = prevNode;
                    closestPreviousRect = prevRect;
                }
            }

            if (closestPreviousNode == null)
                continue;

            closestPreviousNode.AddConnection(currentNode);
            incomingConnections[i]++;

            CreateLine(closestPreviousRect, currentRect);

            Debug.Log(
                $"연결 보정: Floor {currentNode.floor} / " +
                $"Index {currentNode.index}"
            );
        }
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

        // -----------------------------
        // 노드 생성
        // -----------------------------

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

        // -----------------------------
        // Node Seed 생성
        // -----------------------------

        int nodeSeed =
            mapRandom.Next();

        // -----------------------------
        // MapNode 정보 설정
        // -----------------------------

        MapNode mapNode =
            node.GetComponent<MapNode>();

        if (mapNode != null)
        {
            mapNode.Initialize(
                nodeType,
                floor,
                index,
                nodeSeed
            );
        }

        Debug.Log(
            $"Create Node : " +
            $"Floor {floor} / " +
            $"Index {index} / " +
            $"Type {nodeType} / " +
            $"Node Seed {nodeSeed}"
        );

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