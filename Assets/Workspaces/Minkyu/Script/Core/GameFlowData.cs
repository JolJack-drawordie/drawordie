public static class GameFlowData
{
    // 마지막으로 클리어한 층
    public static int clearedNodeLevel = 0;

    // 현재 선택한 노드 정보
    public static int currentFloor = -1;
    public static int currentIndex = -1;

    public static MapNode.NodeType currentNodeType;

    public static void SelectNode(MapNode node)
    {
        currentFloor = node.floor;
        currentIndex = node.index;
        currentNodeType = node.nodeType;
    }
}