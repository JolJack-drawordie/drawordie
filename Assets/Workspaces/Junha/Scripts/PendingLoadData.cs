// ⭐ [로드 기능] 신규 파일
// 로비에서 로드 버튼을 눌렀을 때 서버에서 받아온 세이브 데이터를
// 전투 씬으로 넘어갈 때까지 들고 있는 정적 임시 저장소.
public static class PendingLoadData
{
    public static bool isPending = false;

    public static int hp;
    public static int shield;
    public static int cost;

    public static string deckDataJson;
    public static string monsterDataJson;

    public static void Set(int hp, int shield, int cost, string deckDataJson, string monsterDataJson)
    {
        isPending = true;
        PendingLoadData.hp = hp;
        PendingLoadData.shield = shield;
        PendingLoadData.cost = cost;
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
