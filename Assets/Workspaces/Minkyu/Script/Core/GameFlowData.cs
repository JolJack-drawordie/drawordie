public static class GameFlowData
{
    // 마지막으로 클리어한 층
    public static int clearedNodeLevel = 0;

    // 현재 선택한 노드 정보
    public static int currentFloor = -1;
    public static int currentIndex = -1;

    public static MapNode.NodeType currentNodeType;

    // 현재 선택한 노드의 전용 Seed (MapGenerator가 생성)
    public static int currentNodeSeed = 0;

    // =========================
    // Master Seed
    // =========================

    // 게임 전체를 결정하는 최상위 Seed
    public static int masterSeed = 0;

    // Master Seed가 생성되었는지
    public static bool hasMasterSeed = false;

    // =========================
    // Map Seed
    // =========================

    // Master Seed에서 파생된 Map 전용 Seed
    public static int mapSeed = 0;

    // Map Seed가 생성되었는지
    public static bool hasMapSeed = false;


    // =========================
    // Node 선택
    // =========================

    public static void SelectNode(MapNode node)
    {
        currentFloor = node.floor;
        currentIndex = node.index;
        currentNodeType = node.nodeType;
        currentNodeSeed = node.nodeSeed;
    }


    // =========================
    // Master Seed
    // =========================

    public static void SetMasterSeed(int seed)
    {
        masterSeed = seed;
        hasMasterSeed = true;
    }


    // =========================
    // Map Seed
    // =========================

    public static void SetMapSeed(int seed)
    {
        mapSeed = seed;
        hasMapSeed = true;
    }
}