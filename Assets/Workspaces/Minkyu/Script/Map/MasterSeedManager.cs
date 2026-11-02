using UnityEngine;

// 이번 판 전체를 결정하는 마스터 시드를 정한다 (이미 있으면 그대로, 없으면 새로 만듦).
// MapSeedGenerator보다 먼저 실행되도록 실행 순서를 앞당겼다.
[DefaultExecutionOrder(-100)]
public class MasterSeedManager : MonoBehaviour
{
    [Header("Master Seed")]
    public int masterSeed;

    [Header("랜덤 생성 여부")]
    public bool useRandomSeed = true; // 끄면 인스펙터에 적은 시드로 고정 (테스트용)

    private void Awake()
    {
        GenerateMasterSeed();
    }

    void GenerateMasterSeed()
    {
        if (GameFlowData.hasMasterSeed)
        {
            masterSeed =
                GameFlowData.masterSeed;

            Debug.Log(
                "기존 Master Seed 사용 : " +
                masterSeed
            );
        }
        else
        {
            if (useRandomSeed)
            {
                masterSeed =
                    Random.Range(
                        100000,
                        999999
                    );
            }

            GameFlowData.SetMasterSeed(
                masterSeed
            );

            Debug.Log(
                "새로운 Master Seed 생성 : " +
                masterSeed
            );
        }
    }
}