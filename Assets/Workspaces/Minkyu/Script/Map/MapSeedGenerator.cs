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
        if (useRandomSeed)
        {
            seed = Random.Range(100000, 999999);
        }

        Debug.Log("Map Seed : " + seed);
    }
}