// ⭐ [로드 기능] 신규 파일
// 로비에서 로드 버튼을 눌렀을 때 서버에서 받아온 세이브 데이터를
// 전투 씬으로 넘어갈 때까지 들고 있는 정적 임시 저장소.
public static class PendingLoadData
{
    public static bool isPending = false;

    public static int hp;
    public static int shield;
    public static int cost;
    public static int maxCost;

    public static string deckDataJson;
    public static string monsterDataJson;

    // 휴식 씬 세이브를 불러왔을 때의 덱 (DeckManager/카드 데이터는 전투 씬에만 있으므로 다음 전투 시작 때 복원)
    // isPending과 별개로 유지되며, 복원 전에 다시 저장하면 이 값을 그대로 저장한다.
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
