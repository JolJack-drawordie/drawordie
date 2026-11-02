// 로비에서 '불러오기'로 받은 전투 세이브를 전투 씬이 시작될 때까지 들고 있는 임시 저장소.
// 전투 씬의 TurnManager / DeckManager가 이 값을 꺼내 복원한 뒤 비운다.
public static class PendingLoadData
{
    public static bool isPending = false; // 복원할 전투 세이브가 있는지

    public static int hp;
    public static int shield;
    public static int cost;
    public static int maxCost;

    public static string deckDataJson;
    public static string monsterDataJson;

    // 휴식 씬 세이브를 불러왔을 때의 덱. 덱 매니저와 카드 데이터는 전투 씬에만 있어서 다음 전투가 시작될 때 복원한다.
    // isPending과 따로 유지되고, 복원되기 전에 다시 저장하면 이 값을 그대로 저장한다.
    public static string pendingDeckJson;

    public static void Set(int hp, int shield, int cost, int maxCost, string deckDataJson, string monsterDataJson)
    {
        isPending = true;
        PendingLoadData.hp = hp;
        PendingLoadData.shield = shield;
        PendingLoadData.cost = cost;
        PendingLoadData.maxCost = maxCost;
        PendingLoadData.deckDataJson = deckDataJson;
        PendingLoadData.monsterDataJson = monsterDataJson;
    }

    public static void Clear()
    {
        isPending = false;
        deckDataJson = null;
        monsterDataJson = null;
    }
}
