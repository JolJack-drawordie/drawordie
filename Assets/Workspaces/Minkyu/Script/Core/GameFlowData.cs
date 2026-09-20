
public static class GameFlowData
{
    // =========================
    // Act 정보
    // =========================

    // 현재 진행 중인 Act
    public static int currentAct = 1;

    // 전체 Act 개수
    public const int maxAct = 3;


    // =========================
    // 맵 진행 정보
    // =========================

    // 마지막으로 클리어한 층
    public static int clearedNodeLevel = 0;

    // 현재 선택한 노드 정보
    public static int currentFloor = -1;
    public static int currentIndex = -1;

    public static MapNode.NodeType currentNodeType;

    // 현재 선택한 노드의 전용 Seed
    // MapGenerator가 생성
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
    // Act 관리
    // =========================

    // 다음 Act로 이동
    public static void MoveToNextAct()
    {
        if (currentAct < maxAct)
        {
            currentAct++;

            // 새로운 Act에서 맵 진행 정보 초기화
            clearedNodeLevel = 0;
            currentFloor = -1;
            currentIndex = -1;
            currentNodeType = default;
            currentNodeSeed = 0;

            // 새로운 Act의 Map Seed를 다시 생성할 수 있도록 설정
            hasMapSeed = false;
            mapSeed = 0;
        }
    }

    // 현재 Act가 마지막 Act인지 확인
    public static bool IsFinalAct()
    {
        return currentAct >= maxAct;
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