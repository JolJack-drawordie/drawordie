// 한 판(런) 동안 씬 사이에서 유지되는 진행 정보: Act, 맵 위치, 노드 종류, 시드, 처치 기록, 테스트 설정.
// 세이브 / 로드와 맵 / 전투 / 휴식 / 월드맵 씬이 함께 읽고 쓴다.
public static class GameFlowData
{
    // ===== Act =====

    // 지금 진행 중인 Act (1부터)
    public static int currentAct = 1;

    // 전체 Act 개수
    public const int maxAct = 3;

    // ===== 맵 진행 =====

    // 마지막으로 클리어한 층
    public static int clearedNodeLevel = 0;

    // 지금 고른 노드 (층은 0부터, 맵에서 고르기 전이면 -1)
    public static int currentFloor = -1;
    public static int currentIndex = -1;

    public static MapNode.NodeType currentNodeType;

    // 지금 휴식 노드에서 이미 휴식했는지 (새 노드를 고르면 초기화, 세이브에 저장됨)
    public static bool hasRested = false;

    // 지금 고른 노드의 전용 시드 (MapGenerator가 만듦, 몬스터 선택 등에 사용)
    public static int currentNodeSeed = 0;

    // 이번 판에서 처치한 중간보스 수 (랭킹 점수에 반영, 세이브에 저장됨)
    public static int eliteKills = 0;

    // 월드맵에서 안개 걷힘 연출을 마지막으로 보여 준 Act (0이면 아직 없음, 새 게임 시 초기화)
    public static int worldMapRevealedAct = 0;

    // ===== 마스터 시드 =====

    // 게임 전체를 결정하는 최상위 시드
    public static int masterSeed = 0;

    public static bool hasMasterSeed = false;

    // ===== 맵 시드 =====

    // 마스터 시드에서 파생된 Act별 맵 전용 시드
    public static int mapSeed = 0;

    public static bool hasMapSeed = false;

    // ===== 노드 선택 =====

    // 맵에서 노드를 골랐을 때 위치 / 종류 / 시드 기록
    public static void SelectNode(MapNode node)
    {
        currentFloor = node.floor;
        currentIndex = node.index;
        currentNodeType = node.nodeType;
        currentNodeSeed = node.nodeSeed;
        hasRested = false;
    }

    // ===== Act 관리 =====

    // 다음 Act로: 맵 진행 정보를 비우고 새 맵 시드를 만들 수 있게 한다
    public static void MoveToNextAct()
    {
        if (currentAct < maxAct)
        {
            currentAct++;

            clearedNodeLevel = 0;
            currentFloor = -1;
            currentIndex = -1;
            currentNodeType = default;
            currentNodeSeed = 0;

            hasMapSeed = false;
            mapSeed = 0;
        }
    }

    // 새 게임: 이전 판(로비로 돌아온 판 등)의 진행 정보와 시드를 모두 지운다
    public static void ResetRun()
    {
        currentAct = 1;

        clearedNodeLevel = 0;
        currentFloor = -1;
        currentIndex = -1;
        currentNodeType = default;
        currentNodeSeed = 0;
        hasRested = false;
        eliteKills = 0;
        worldMapRevealedAct = 0;

        masterSeed = 0;
        hasMasterSeed = false;
        mapSeed = 0;
        hasMapSeed = false;

        debugUnlockBoss = false;
        debugBigDamage = false;
    }

    // 지정한 Act의 맵 처음부터 시작 (테스트용, LobbyManager 테스트 설정)
    public static void StartAtAct(int act)
    {
        currentAct = act < 1 ? 1 : (act > maxAct ? maxAct : act);

        clearedNodeLevel = 0;
        currentFloor = -1;
        currentIndex = -1;
        currentNodeType = default;
        currentNodeSeed = 0;

        hasMapSeed = false;
        mapSeed = 0;
    }

    // 테스트용: 맵 시작 시 0층 대신 보스 노드를 바로 고를 수 있게
    public static bool debugUnlockBoss = false;

    // 테스트용: 플레이어가 적에게 주는 피해를 최소 이 값으로 (LobbyManager 테스트 설정, 빌드에서는 항상 꺼짐)
    public static bool debugBigDamage = false;
    public const int DebugBigDamageAmount = 150;

    // 지금 Act가 마지막 Act인지
    public static bool IsFinalAct()
    {
        return currentAct >= maxAct;
    }

    // ===== 시드 설정 =====

    public static void SetMasterSeed(int seed)
    {
        masterSeed = seed;
        hasMasterSeed = true;
    }

    public static void SetMapSeed(int seed)
    {
        mapSeed = seed;
        hasMapSeed = true;
    }
}