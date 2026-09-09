using UnityEngine;

[DefaultExecutionOrder(-100)]
public class MasterSeedManager : MonoBehaviour
{
    [Header("Master Seed")]
    public int masterSeed;

    [Header("랜덤 생성 여부")]
    public bool useRandomSeed = true;

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