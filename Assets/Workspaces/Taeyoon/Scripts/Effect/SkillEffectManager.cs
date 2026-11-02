using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 카드 스킬 이펙트. 동명사(동작) 이펙트를 먼저 재생하고, 형용사(속성)가 붙은 카드면 속성 이펙트를 겹쳐 재생한다.
// 씬이 바뀌어도 유지된다.
public class SkillEffectManager : MonoBehaviour
{
    public static SkillEffectManager Instance;

    // 카드 id ↔ 이펙트 프리팹
    [System.Serializable]
    public class EffectData
    {
        public int id;
        public string name; // 인스펙터 목록에 보이는 이름
        public GameObject effectPrefab;
    }

    [Header("형용사(속성) 이펙트 리스트 (101~110)")]
    public List<EffectData> adjectiveEffects = new List<EffectData>();

    [Header("동명사(동작) 이펙트 리스트 (201~210)")]
    public List<EffectData> gerundEffects = new List<EffectData>();

    // id로 빨리 찾기 위한 표
    private Dictionary<int, GameObject> adjectiveDict = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> gerundDict = new Dictionary<int, GameObject>();

    // 위치가 없거나 화면 밖 값이면 쓰는 기본 위치 (적 자리)
    private readonly Vector3 defaultSlimePosition = new Vector3(4.93f, -1.73f, -1f);

    private readonly WaitForSeconds waitDelay = new WaitForSeconds(0.05f); // 동명사 이펙트 뒤에 형용사 이펙트를 겹칠 때의 간격

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

    // 인스펙터 목록 → id 표 (프리팹이 없거나 중복된 id는 건너뜀)
    private void InitDictionaries()
    {
        adjectiveDict.Clear();
        foreach (var data in adjectiveEffects)
        {
            if (data.effectPrefab != null && data.id > 0 && !adjectiveDict.ContainsKey(data.id))
            {
                adjectiveDict.Add(data.id, data.effectPrefab);
            }
        }

        gerundDict.Clear();
        foreach (var data in gerundEffects)
        {
            if (data.effectPrefab != null && data.id > 0 && !gerundDict.ContainsKey(data.id))
            {
                gerundDict.Add(data.id, data.effectPrefab);
            }
        }
    }

    // 카드를 쓸 때 호출 (형용사 id가 0 이하면 동명사 이펙트만)
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

    // 동명사 이펙트 → 잠깐 뒤 형용사 이펙트 (각각 일정 시간 뒤 자동 삭제)
    private IEnumerator RoutinePlayCombinedEffect(int adjectiveId, int gerundId, Vector3 targetPosition)
    {
        // 1. 동작(동명사) 이펙트
        if (gerundDict.TryGetValue(gerundId, out GameObject gerundPrefab))
        {
            GameObject gerundInstance = Instantiate(gerundPrefab, targetPosition, Quaternion.identity);
            SetSortingOrderHigh(gerundInstance);
            Destroy(gerundInstance, 2.5f);
        }

        // 2. 형용사 id가 있고 이펙트가 등록돼 있으면 속성 이펙트
        if (adjectiveId > 0 && adjectiveDict.TryGetValue(adjectiveId, out GameObject adjPrefab))
        {
            yield return waitDelay;

            GameObject adjInstance = Instantiate(adjPrefab, targetPosition, Quaternion.identity);
            SetSortingOrderHigh(adjInstance);
            Destroy(adjInstance, 0.5f);
        }
    }

    // 캐릭터 / 배경보다 위에 그려지도록
    private void SetSortingOrderHigh(GameObject effectObj)
    {
        Renderer[] renderers = effectObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.sortingOrder = 1000;
        }
    }
}