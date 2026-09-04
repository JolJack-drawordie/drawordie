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

    [Header("Connection Settings")]
    [Range(1, 2)]
    public int minConnections = 1;

    [Range(1, 2)]
    public int maxConnections = 2;

    private MapSeedGenerator seedGenerator;

    private void Awake()
    {
        seedGenerator = GetComponent<MapSeedGenerator>();

        if (seedGenerator != null)
        {
            Random.InitState(seedGenerator.seed);
        }

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
    /// 이전 층과 현재 층을 1~2개의 연결로 연결
    /// </summary>
    void CreateConnections(
        List<GameObject> previousFloor,
        List<GameObject> currentFloor)
    {
        // 각 현재 노드의 연결 개수 확인용
        int[] incomingConnections =
            new int[currentFloor.Count];

        // -----------------------------
        // 1단계
        // 이전 층의 각 노드가
        // 다음 층의 1~2개 노드와 연결
        // -----------------------------

        foreach (GameObject prev in previousFloor)
        {
            MapNode prevNode =
                prev.GetComponent<MapNode>();

            if (prevNode == null)
                continue;

            // 연결할 개수 결정
            int connectionCount =
                Random.Range(
                    minConnections,
                    maxConnections + 1
                );

            // 현재 층 노드가 1개라면 1개만 연결
            connectionCount =
                Mathf.Min(
                    connectionCount,
                    currentFloor.Count
                );

            // 중복 방지
            List<int> selectedIndexes =
                new List<int>();

            while (
                selectedIndexes.Count <
                connectionCount)
            {
                int randomIndex =
                    Random.Range(
                        0,
                        currentFloor.Count
                    );

                if (!selectedIndexes.Contains(
                    randomIndex))
                {
                    selectedIndexes.Add(
                        randomIndex
                    );
                }
            }

            foreach (int index
                in selectedIndexes)
            {
                MapNode currentNode =
                    currentFloor[index]
                        .GetComponent<MapNode>();

                if (currentNode == null)
                    continue;

                prevNode.AddConnection(
                    currentNode
                );

                incomingConnections[index]++;

                CreateLine(
                    prev.GetComponent<RectTransform>(),
                    currentFloor[index]
                        .GetComponent<RectTransform>()
                );
            }
        }

        // -----------------------------
        // 2단계
        // 연결되지 않은 현재 층 노드가
        // 있다면 이전 층의 랜덤 노드와 연결
        // -----------------------------

        for (int i = 0;
             i < currentFloor.Count;
             i++)
        {
            if (incomingConnections[i] > 0)
                continue;

            // 연결되지 않은 현재 노드
            GameObject current =
                currentFloor[i];

            // 이전 층에서 랜덤 노드 선택
            int previousIndex =
                Random.Range(
                    0,
                    previousFloor.Count
                );

            GameObject previous =
                previousFloor[previousIndex];

            MapNode previousNode =
                previous.GetComponent<MapNode>();

            MapNode currentNode =
                current.GetComponent<MapNode>();

            if (previousNode == null ||
                currentNode == null)
            {
                continue;
            }

            // 연결 추가
            previousNode.AddConnection(
                currentNode
            );

            incomingConnections[i]++;

            // 선 생성
            CreateLine(
                previous.GetComponent<RectTransform>(),
                current.GetComponent<RectTransform>()
            );

            Debug.Log(
                $"연결되지 않은 노드 보정 : " +
                $"Floor {currentNode.floor} / " +
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