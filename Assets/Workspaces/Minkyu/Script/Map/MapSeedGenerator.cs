using UnityEngine;

// 지금 Act의 맵 시드를 정한다 (이미 있으면 그대로, 없으면 마스터 시드 + Act로 새로 만듦).
// 같은 마스터 시드면 Act마다 항상 같은 맵이 나오고, 이 시드로 Unity 난수를 초기화해 MapGenerator가 쓴다.
public class MapSeedGenerator : MonoBehaviour
{
    [Header("Seed")]
    public int seed; // 이번 Act의 맵 시드 (확인용)

    private MasterSeedManager masterSeedManager;

    private void Awake()
    {
        masterSeedManager = GetComponent<MasterSeedManager>();

        GenerateMapSeed();
    }

    void GenerateMapSeed()
    {
        if (masterSeedManager == null)
        {
            Debug.LogError("MasterSeedManager가 없습니다.");
            return;
        }

        int masterSeed = masterSeedManager.masterSeed;
        int currentAct = GameFlowData.currentAct;

        // 이 Act의 맵 시드가 이미 있으면(같은 Act에서 맵으로 돌아온 경우, 세이브 로드) 그대로 사용
        if (GameFlowData.hasMapSeed)
        {
            seed = GameFlowData.mapSeed;

            Debug.Log(
                $"기존 Map Seed 사용 : {seed} " +
                $"(Act {currentAct})"
            );
        }
        else
        {
            // 마스터 시드와 Act를 섞어 Act마다 다른 맵 시드
            int actSeed =
                masterSeed + (currentAct * 1000003);

            System.Random random =
                new System.Random(actSeed);

            seed = random.Next(100000, 999999);

            GameFlowData.SetMapSeed(seed);

            Debug.Log(
                $"Master Seed : {masterSeed}"
            );

            Debug.Log(
                $"Current Act : {currentAct}"
            );

            Debug.Log(
                $"새로운 Map Seed 생성 : {seed}"
            );
        }

        // MapGenerator가 쓸 난수 상태를 이 시드로 초기화
        UnityEngine.Random.InitState(seed);
    }
}