using UnityEngine;

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
        // 이미 Master Seed가 존재한다면 기존 Seed 사용
        if (GameFlowData.hasMasterSeed)
        {
            masterSeed = GameFlowData.masterSeed;

            Debug.Log("기존 Master Seed 사용 : " + masterSeed);
        }
        else
        {
            // 새로운 Master Seed 생성
            if (useRandomSeed)
            {
                masterSeed = Random.Range(100000, 999999);
            }

            // GameFlowData에 저장
            GameFlowData.SetMasterSeed(masterSeed);

            Debug.Log("새로운 Master Seed 생성 : " + masterSeed);
        }
    }
}