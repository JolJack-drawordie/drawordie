using UnityEngine;

public class MapSeedGenerator : MonoBehaviour
{
    [Header("Seed")]
    public int seed;

    [Header("랜덤 생성 여부")]
    public bool useRandomSeed = true;

    private void Awake()
    {
        GenerateSeed();
    }

    void GenerateSeed()
    {
        // 이미 저장된 Seed가 있다면 기존 Seed 사용
        if (GameFlowData.hasMapSeed)
        {
            seed = GameFlowData.mapSeed;

            Debug.Log("기존 Map Seed 사용 : " + seed);
        }
        else
        {
            // 새로운 Seed 생성
            if (useRandomSeed)
            {
                seed = Random.Range(100000, 999999);
            }

            // GameFlowData에 Seed 저장
            GameFlowData.SetMapSeed(seed);

            Debug.Log("새로운 Map Seed 생성 : " + seed);
        }

        // 해당 Seed를 기준으로 랜덤 초기화
        Random.InitState(seed);
    }
}