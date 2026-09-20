using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectManager : MonoBehaviour
{
    public static SkillEffectManager Instance;

    [System.Serializable]
    public class EffectData
    {
        public int id;
        public string name;
        public GameObject effectPrefab;
    }

    [Header("형용사(속성) 이펙트 리스트 (101~110)")]
    public List<EffectData> adjectiveEffects = new List<EffectData>();

    [Header("동명사(동작) 이펙트 리스트 (201~210)")]
    public List<EffectData> gerundEffects = new List<EffectData>();

    private Dictionary<int, GameObject> adjectiveDict = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> gerundDict = new Dictionary<int, GameObject>();

    // 기본 슬라임 타격 위치 지정 (X: 4.93, Y: -1.73, Z: -1)
    private readonly Vector3 defaultSlimePosition = new Vector3(4.93f, -1.73f, -1f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitDictionaries();
    }

    private void InitDictionaries()
    {
        adjectiveDict.Clear();
        foreach (var data in adjectiveEffects)
        {
            if (data.effectPrefab != null && !adjectiveDict.ContainsKey(data.id))
            {
                adjectiveDict.Add(data.id, data.effectPrefab);
            }
        }

        gerundDict.Clear();
        foreach (var data in gerundEffects)
        {
            if (data.effectPrefab != null && !gerundDict.ContainsKey(data.id))
            {
                gerundDict.Add(data.id, data.effectPrefab);
            }
        }
    }

    /// <summary>
    /// 스킬 이펙트를 재생합니다.
    /// 별도 좌표 지정이 없거나 UI 좌표가 오면 슬라임 위치(4.93, -1.73, -1)에서 터집니다.
    /// </summary>
    public void PlaySkillEffect(int adjectiveId, int gerundId, Vector3 targetPosition = default)
    {
        if (targetPosition == default || Mathf.Abs(targetPosition.x) > 100f || Mathf.Abs(targetPosition.y) > 100f)
        {
            targetPosition = defaultSlimePosition;
        }
        else
        {
            targetPosition.z = -1f;
        }

        StartCoroutine(RoutinePlayCombinedEffect(adjectiveId, gerundId, targetPosition));
    }

    private IEnumerator RoutinePlayCombinedEffect(int adjectiveId, int gerundId, Vector3 targetPosition)
    {
        // 1. 동작(동명사) 이펙트
        if (gerundDict.TryGetValue(gerundId, out GameObject gerundPrefab))
        {
            GameObject gerundInstance = Instantiate(gerundPrefab, targetPosition, Quaternion.identity);
            SetSortingOrderHigh(gerundInstance);

            // 0.5초 뒤 자동 삭제
            Destroy(gerundInstance, 2.5f);
        }

        yield return new WaitForSeconds(0.05f);

        // 2. 속성(형용사) 이펙트
        if (adjectiveDict.TryGetValue(adjectiveId, out GameObject adjPrefab))
        {
            GameObject adjInstance = Instantiate(adjPrefab, targetPosition, Quaternion.identity);
            SetSortingOrderHigh(adjInstance);

            // 0.5초 뒤 자동 삭제
            Destroy(adjInstance, 0.5f);
        }
    }

    private void SetSortingOrderHigh(GameObject effectObj)
    {
        Renderer[] renderers = effectObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.sortingOrder = 1000;
        }
    }
}