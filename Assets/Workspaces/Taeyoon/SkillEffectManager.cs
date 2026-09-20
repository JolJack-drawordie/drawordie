using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillEffectManager : MonoBehaviour
{
    public static SkillEffectManager Instance;
    private const int EffectLayer = 31;

    private RenderTexture effectTexture;

    [System.Serializable]
    public struct EffectData
    {
        public int id;                  // adjectiveId 또는 gerundId (예: 101, 201)
        public string name;            // 에디터 식별용 이름 (예: "101_불타는", "201_베기")
        public GameObject effectPrefab;// 재생할 파티클 프리팹
    }

    [Header("형용사(속성) 이펙트 리스트 (101~110)")]
    public List<EffectData> adjectiveEffects;

    [Header("동명사(동작) 이펙트 리스트 (201~210)")]
    public List<EffectData> gerundEffects;

    private Dictionary<int, GameObject> adjectiveDict;
    private Dictionary<int, GameObject> gerundDict;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // 딕셔너리 동기화로 빠른 검색
        adjectiveDict = new Dictionary<int, GameObject>();
        foreach (var item in adjectiveEffects)
        {
            if (item.effectPrefab != null && !adjectiveDict.ContainsKey(item.id))
                adjectiveDict.Add(item.id, item.effectPrefab);
        }

        gerundDict = new Dictionary<int, GameObject>();
        foreach (var item in gerundEffects)
        {
            if (item.effectPrefab != null && !gerundDict.ContainsKey(item.id))
                gerundDict.Add(item.id, item.effectPrefab);
        }

        SetupEffectOverlay();
    }

    private void SetupEffectOverlay()
    {
        Camera mainCamera = Camera.main;
        Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
        if (mainCamera == null || canvas == null)
        {
            Debug.LogError("[SkillEffectManager] Main Camera or Canvas was not found.");
            return;
        }

        // ponytail: layer 31 is currently unused; reserve a named layer if layer policy grows.
        mainCamera.cullingMask &= ~(1 << EffectLayer);

        GameObject cameraObject = new GameObject("SkillEffectCamera");
        cameraObject.transform.SetParent(mainCamera.transform, false);
        Camera effectCamera = cameraObject.AddComponent<Camera>();
        effectCamera.CopyFrom(mainCamera);
        effectCamera.clearFlags = CameraClearFlags.SolidColor;
        effectCamera.backgroundColor = Color.clear;
        effectCamera.cullingMask = 1 << EffectLayer;

        effectTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32)
        {
            name = "SkillEffectTexture"
        };
        effectTexture.Create();
        effectCamera.targetTexture = effectTexture;

        GameObject overlay = new GameObject("SkillEffectOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        RectTransform rect = overlay.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = overlay.GetComponent<RawImage>();
        image.texture = effectTexture;
        image.raycastTarget = false;
        overlay.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 서버에서 받은 adjectiveId와 gerundId로 이펙트 합성 재생
    /// </summary>
    public void PlaySkillEffect(int adjectiveId, int gerundId, Vector3 targetPosition)
    {
        PlayEffect(adjectiveDict, adjectiveId, targetPosition);
        PlayEffect(gerundDict, gerundId, targetPosition);
    }

    private void PlayEffect(Dictionary<int, GameObject> effects, int id, Vector3 position)
    {
        if (!effects.TryGetValue(id, out GameObject prefab))
        {
            Debug.LogWarning($"[SkillEffectManager] Effect ID {id} is not configured.");
            return;
        }

        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        SetLayer(effect.transform, EffectLayer);
        foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            particle.Play(true);

        Destroy(effect, 2.5f);
    }

    private static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayer(child, layer);
    }

    private void OnDestroy()
    {
        if (effectTexture != null)
            effectTexture.Release();
    }
}
