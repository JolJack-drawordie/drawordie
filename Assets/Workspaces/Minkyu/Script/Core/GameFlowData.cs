using System.Collections.Generic;

public static class GameFlowData
{
    // 마지막으로 클리어한 층
    public static int clearedNodeLevel = 0;

    // 현재 선택한 노드 정보
    public static int currentFloor = -1;
    public static int currentIndex = -1;

    public static MapNode.NodeType currentNodeType;

    // 맵 Seed
    public static int mapSeed = 0;

    // Seed가 이미 생성되었는지
    public static bool hasMapSeed = false;

    // 방문한 노드 목록
    private static HashSet<string> visitedNodes =
        new HashSet<string>();

    /// <summary>
    /// 노드 선택
    /// </summary>
    public static void SelectNode(MapNode node)
    {
        currentFloor = node.floor;
        currentIndex = node.index;
        currentNodeType = node.nodeType;
    }

    /// <summary>
    /// 맵 Seed 저장
    /// </summary>
    public static void SetMapSeed(int seed)
    {
        mapSeed = seed;
        hasMapSeed = true;
    }

    /// <summary>
    /// 노드 방문 처리
    /// </summary>
    public static void AddVisitedNode(MapNode node)
    {
        if (node == null)
            return;

        string nodeKey =
            GetNodeKey(node.floor, node.index);

        visitedNodes.Add(nodeKey);

        UnityEngine.Debug.Log(
            $"방문 노드 저장 : Floor {node.floor} / Index {node.index}"
        );
    }

    /// <summary>
    /// 해당 노드를 방문했는지 확인
    /// </summary>
    public static bool IsNodeVisited(MapNode node)
    {
        if (node == null)
            return false;

        string nodeKey =
            GetNodeKey(node.floor, node.index);

        return visitedNodes.Contains(nodeKey);
    }

    /// <summary>
    /// Floor + Index를 이용해서 노드 고유 키 생성
    /// </summary>
    private static string GetNodeKey(
        int floor,
        int index)
    {
        return floor + "_" + index;
    }

    /// <summary>
    /// 모든 진행 상태 초기화
    /// </summary>
    public static void ResetProgress()
    {
        clearedNodeLevel = 0;

        currentFloor = -1;
        currentIndex = -1;

        currentNodeType =
            MapNode.NodeType.Monster;

        mapSeed = 0;
        hasMapSeed = false;

        visitedNodes.Clear();

        UnityEngine.Debug.Log(
            "GameFlowData 진행 상태 초기화"
        );
    }
}