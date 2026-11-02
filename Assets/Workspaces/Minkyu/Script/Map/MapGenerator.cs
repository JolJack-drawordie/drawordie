using System;
using System.Collections.Generic;
using UnityEngine;

// 맵 씬에 들어올 때 맵 시드로 노드와 연결선을 만든다 (같은 시드면 항상 같은 맵).
// - 층마다 노드 3~4개, 마지막 층은 보스, 보스 바로 전 층은 전부 휴식
// - 2층부터 휴식 / 중간보스가 무작위로 섞이고, 중간보스는 Act마다 한 번은 꼭 나온다
// - 이전 층 노드는 가로로 가까운 노드부터 이어지고, 들어오는 길이 없는 노드는 가장 가까운 노드와 이어 준다
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

    [Header("Rest Settings")] // 일반 노드가 휴식 노드가 될 확률
    [Range(0f, 1f)]
    public float restChance = 0.2f;

    [Header("Connection Settings")]
    // 이전 층 노드 하나가 다음 층과 이어지는 개수 범위
    [Range(1, 2)]
    public int minConnections = 1;

    [Range(1, 2)]
    public int maxConnections = 2;

    // 맵 시드로 만든 맵 전용 난수 (노드 수, 종류, 연결, 노드 시드)

    private System.Random mapRandom;

    private MapSeedGenerator mapSeedGenerator;

    private void Awake()
    {
        // 같은 오브젝트의 MapSeedGenerator가 이 Act의 맵 시드를 정해 둔다
        mapSeedGenerator =
            GetComponent<MapSeedGenerator>();

        if (mapSeedGenerator == null)
        {
            Debug.LogError(
                "MapGenerator : MapSeedGenerator가 없습니다."
            );

            return;
        }

        int mapSeed =
            mapSeedGenerator.seed;

        // 맵 시드로 맵 전용 난수를 만든다
        mapRandom =
            new System.Random(mapSeed);

        Debug.Log(
            "MapGenerator Map Seed : " +
            mapSeed
        );

        // 맵 생성
        GenerateLayout();
    }

    // 층마다 노드를 만들고 이전 층과 잇는다
    void GenerateLayout()
    {
        // 이전 맵의 노드 / 선 지우기
        foreach (Transform child in nodeParent)
        {
            Destroy(child.gameObject);
        }

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

            // ----- 마지막 층 = 보스 -----

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
                // ----- 일반 층: 노드 3~4개 -----

                int nodeCount =
                    mapRandom.Next(3, 5);

                // 보스 바로 전 층인지

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

                    // 보스 바로 전 층은 모든 노드를 휴식으로

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
                        // ----- 2층부터 휴식 / 중간보스 섞기 -----

                        if (floor >= 2)
                        {
                            // 이번 Act에서 중간보스가 이미 나왔는지 (이전 층과 지금 층 확인)
                            bool eliteAlreadyCreated = false;

                            foreach (GameObject previousNode in previousFloor)
                            {
                                if (previousNode == null) continue;

                                MapNode previousMapNode =
                                    previousNode.GetComponent<MapNode>();

                                if (previousMapNode != null &&
                                    previousMapNode.nodeType == MapNode.NodeType.Elite)
                                {
                                    eliteAlreadyCreated = true;
                                    break;
                                }
                            }

                            foreach (GameObject currentNode in currentFloor)
                            {
                                if (currentNode == null) continue;

                                MapNode currentMapNode =
                                    currentNode.GetComponent<MapNode>();

                                if (currentMapNode != null &&
                                    currentMapNode.nodeType == MapNode.NodeType.Elite)
                                {
                                    eliteAlreadyCreated = true;
                                    break;
                                }
                            }

                            // 아직 중간보스가 없으면: 마지막 일반 층의 마지막 노드는 반드시, 그 전에는 확률로
                            if (!eliteAlreadyCreated)
                            {
                                bool isLastNormalFloor =
                                    floor == floorCount - 3;

                                bool isLastNode =
                                    i == nodeCount - 1;

                                if (isLastNormalFloor && isLastNode)
                                {
                                    prefab = elitePrefab;
                                    nodeType = MapNode.NodeType.Elite;
                                }
                                else
                                {
                                    double randomValue = mapRandom.NextDouble();

                                    if (randomValue < restChance)
                                    {
                                        prefab = restPrefab;
                                        nodeType = MapNode.NodeType.Rest;
                                    }
                                    else if (randomValue < restChance + 0.25f)
                                    {
                                        prefab = elitePrefab;
                                        nodeType = MapNode.NodeType.Elite;
                                    }
                                }
                            }
                            // 중간보스가 이미 있으면 휴식만 무작위로
                            else
                            {
                                if (mapRandom.NextDouble() < restChance)
                                {
                                    prefab = restPrefab;
                                    nodeType = MapNode.NodeType.Rest;
                                }
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

            // ----- 이전 층과 지금 층 잇기 -----

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

    // 이전 층과 지금 층을 잇는다. 가로로 가까운 노드를 우선 잇고, 들어오는 길이 없는 노드는 보정한다
    void CreateConnections(
        List<GameObject> previousFloor,
        List<GameObject> currentFloor)
    {
        if (previousFloor == null || currentFloor == null)
            return;

        if (previousFloor.Count == 0 || currentFloor.Count == 0)
            return;

        // 지금 층 노드마다 들어오는 연결 수
        int[] incomingConnections = new int[currentFloor.Count];

        // 이전 층 노드마다 가까운 순서로 정해진 개수만큼 연결
        foreach (GameObject prev in previousFloor)
        {
            if (prev == null)
                continue;

            MapNode prevNode = prev.GetComponent<MapNode>();
            RectTransform prevRect = prev.GetComponent<RectTransform>();

            if (prevNode == null || prevRect == null)
                continue;

            // 연결 개수 결정 (설정 범위 안에서 무작위)
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

        // 들어오는 연결이 없는 노드는 이전 층의 가장 가까운 노드와 연결
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

    // 노드 프리팹을 만들고 위치 / 종류 / 노드 시드 지정
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

        // 노드 생성 후 위치 지정

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

        // 노드 전용 시드 (맵 난수에서 뽑으므로 같은 맵이면 같은 값)

        int nodeSeed =
            mapRandom.Next();

        // 노드 정보 설정

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

    // 두 노드 사이에 선 그리기 (가운데에 두고 길이 / 각도를 맞춘 얇은 이미지)
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

        lineRect.anchoredPosition =
            (start + end) / 2f;

        lineRect.sizeDelta =
            new Vector2(
                distance,
                6f
            );

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