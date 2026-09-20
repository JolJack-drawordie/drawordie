
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
        // MasterSeedManager 확인
        if (masterSeedManager == null)
        {
            Debug.LogError("MasterSeedManager가 없습니다.");
            return;
        }

        int masterSeed = masterSeedManager.masterSeed;
        int currentAct = GameFlowData.currentAct;

        // 현재 Act의 Map Seed가 이미 존재한다면 기존 Seed 사용
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
            // Master Seed와 현재 Act를 함께 사용
            // Act마다 서로 다른 Map Seed 생성
            int actSeed =
                masterSeed + (currentAct * 1000003);

            System.Random random =
                new System.Random(actSeed);

            seed = random.Next(100000, 999999);

            // GameFlowData에 저장
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

        // MapGenerator에서 사용할 랜덤 상태 초기화
        UnityEngine.Random.InitState(seed);
    }
}