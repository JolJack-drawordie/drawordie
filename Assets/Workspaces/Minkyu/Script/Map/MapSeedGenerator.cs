using System;
using UnityEngine;

public class MapSeedGenerator : MonoBehaviour
{
    [Header("Seed")]
    public int seed;

    private MasterSeedManager masterSeedManager;

    private void Awake()
    {
        masterSeedManager = GetComponent<MasterSeedManager>();

        GenerateMapSeed();
    }

    void GenerateMapSeed()
    {
        // Master Seed가 같은지 확인
        if (masterSeedManager == null)
        {
            Debug.LogError("MasterSeedManager가 없습니다.");
            return;
        }

        int masterSeed = masterSeedManager.masterSeed;

        // 이미 Map Seed가 있다면 기존 Map Seed 사용
        if (GameFlowData.hasMapSeed)
        {
            seed = GameFlowData.mapSeed;

            Debug.Log("기존 Map Seed 사용 : " + seed);
        }
        else
        {
            // Master Seed를 기반으로 Map 전용 Seed 생성
            System.Random random = new System.Random(masterSeed);

            seed = random.Next(100000, 999999);

            // GameFlowData에 저장
            GameFlowData.SetMapSeed(seed);

            Debug.Log("Master Seed : " + masterSeed);
            Debug.Log("새로운 Map Seed 생성 : " + seed);
        }

        // MapGenerator에서 사용할 랜덤 상태 초기화
        UnityEngine.Random.InitState(seed);
    }
}