using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 월드맵에서 두 섬을 잇는 점선: Points 아래 자식들을 순서대로 모두 지나는 매끄러운 곡선을 따라 점을 찍는다.
// 첫 자식이 출발, 마지막 자식이 도착. 자식을 끌어서 모양을 바꾸고, 복제(Ctrl+D)해서 점을 늘릴 수 있다
// (Scene 뷰에 곡선이 노란 선으로 보임).
// 이미 지나온 길은 처음부터 보이고, 방금 열린 길은 점이 하나씩 톡톡 찍히며 그려진다.
// 점은 섬보다 먼저 그려지므로(계층 순서) 섬과 안개 아래로 들어가 보인다.
public class WorldMapPath : MonoBehaviour
{
    [Header("곡선 (자식들을 순서대로 지나감)")]
    [SerializeField] private Transform pointsRoot;
    [Tooltip("구간 하나를 나누는 개수 (클수록 곡선이 정확)")]
    [SerializeField] private int samplesPerSegment = 30;

    [Header("점")]
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private Color dotColor = Color.white;
    [Tooltip("점 이미지 크기 (이미지 여백 포함)")]
    [SerializeField] private float dotSize = 50f;
    [Tooltip("점 사이 간격")]
    [SerializeField] private float dotSpacing = 32f;
    [Tooltip("점마다 크기가 달라지는 정도 (손으로 찍은 느낌)")]
    [SerializeField] private float dotSizeJitter = 0.15f;

    [Header("그리기 연출")]
    [Tooltip("길 전체가 그려지는 시간")]
    [SerializeField] private float drawTime = 0.8f;
    [Tooltip("점 하나가 튀어나오는 시간")]
    [SerializeField] private float dotPopTime = 0.12f;
    [Tooltip("점이 튀어나올 때 가장 커지는 배율")]
    [SerializeField] private float dotPopScale = 1.4f;
    [Tooltip("점 몇 개마다 점 효과음을 낼지 (1이면 매번)")]
    [SerializeField] private int dotSoundEvery = 1;

    private readonly List<Image> dots = new List<Image>();
    private CanvasGroup canvasGroup;

    // shown: 처음부터 보이는 길이면 true, 이번에 그려질 길이면 false (숨겨 둠)
    public void Init(bool shown)
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        BuildDots();

        foreach (Image dot in dots)
            dot.rectTransform.localScale = shown ? Vector3.one : Vector3.zero;
    }

    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }

    // 점이 시작점부터 하나씩 튀어나오며 길이 그려짐
    public IEnumerator Draw()
    {
        if (dots.Count == 0) yield break;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapPathSound);

        float interval = drawTime / dots.Count;
        int every = Mathf.Max(1, dotSoundEvery);
        float time = 0f;
        int nextDot = 0;

        while (nextDot < dots.Count || time < interval * (dots.Count - 1) + dotPopTime)
        {
            // 이번 프레임까지 나타나야 할 점의 효과음
            while (nextDot < dots.Count && time >= interval * nextDot)
            {
                if (nextDot % every == 0 && SoundManager.Instance != null)
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.worldMapPathDotSound, true);
                nextDot++;
            }

            for (int i = 0; i < nextDot; i++)
            {
                float t = Mathf.Clamp01((time - interval * i) / dotPopTime);
                // 0 → dotPopScale → 1
                float scale = t < 0.5f
                    ? Mathf.Lerp(0f, dotPopScale, t * 2f)
                    : Mathf.Lerp(dotPopScale, 1f, (t - 0.5f) * 2f);
                dots[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            time += Time.deltaTime;
            yield return null;
        }

        foreach (Image dot in dots)
            dot.rectTransform.localScale = Vector3.one;
    }

    // 곡선 길이에 맞춰 간격이 고르게 점을 만든다
    private void BuildDots()
    {
        foreach (Image dot in dots)
        {
            if (dot != null)
                Destroy(dot.gameObject);
        }
        dots.Clear();

        if (pointsRoot == null || dotSprite == null) return;

        // 기준점들을 이 오브젝트 기준 좌표로
        List<Vector3> keys = new List<Vector3>();
        foreach (Transform child in pointsRoot)
            keys.Add(transform.InverseTransformPoint(child.position));
        List<Vector3> curve = SampleCurve(keys);
        if (curve.Count == 0) return;

        // 곡선을 따라가며 dotSpacing마다 점 위치를 고른다
        List<Vector2> points = new List<Vector2> { curve[0] };
        float carried = 0f;
        for (int i = 1; i < curve.Count; i++)
        {
            carried += Vector3.Distance(curve[i - 1], curve[i]);
            if (carried >= dotSpacing)
            {
                points.Add(curve[i]);
                carried = 0f;
            }
        }

        // 길마다 점 크기·회전이 매번 같도록
        Random.State saved = Random.state;
        Random.InitState(name.GetHashCode());

        foreach (Vector2 point in points)
        {
            GameObject go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localPosition = point;
            float size = dotSize * (1f + Random.Range(-dotSizeJitter, dotSizeJitter));
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Image image = go.GetComponent<Image>();
            image.sprite = dotSprite;
            image.color = dotColor;
            image.preserveAspect = true;
            image.raycastTarget = false;
            dots.Add(image);
        }

        Random.state = saved;
    }

    // 기준점을 모두 지나는 곡선 (Centripetal Catmull-Rom: 점 간격이 고르지 않아도 꼬이지 않음)
    private List<Vector3> SampleCurve(List<Vector3> keys)
    {
        List<Vector3> result = new List<Vector3>();
        if (keys.Count == 0) return result;
        if (keys.Count == 1)
        {
            result.Add(keys[0]);
            return result;
        }

        int samples = Mathf.Max(2, samplesPerSegment);
        result.Add(keys[0]);
        for (int i = 0; i < keys.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? keys[i - 1] : keys[i] * 2f - keys[i + 1];
            Vector3 p1 = keys[i];
            Vector3 p2 = keys[i + 1];
            Vector3 p3 = i + 2 < keys.Count ? keys[i + 2] : keys[i + 1] * 2f - keys[i];

            for (int k = 1; k <= samples; k++)
                result.Add(CatmullRom(p0, p1, p2, p3, k / (float)samples));
        }
        return result;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t0 = 0f;
        float t1 = t0 + Mathf.Max(Mathf.Sqrt(Vector3.Distance(p0, p1)), 0.0001f);
        float t2 = t1 + Mathf.Max(Mathf.Sqrt(Vector3.Distance(p1, p2)), 0.0001f);
        float t3 = t2 + Mathf.Max(Mathf.Sqrt(Vector3.Distance(p2, p3)), 0.0001f);
        float u = Mathf.Lerp(t1, t2, t);

        Vector3 a1 = (t1 - u) / (t1 - t0) * p0 + (u - t0) / (t1 - t0) * p1;
        Vector3 a2 = (t2 - u) / (t2 - t1) * p1 + (u - t1) / (t2 - t1) * p2;
        Vector3 a3 = (t3 - u) / (t3 - t2) * p2 + (u - t2) / (t3 - t2) * p3;
        Vector3 b1 = (t2 - u) / (t2 - t0) * a1 + (u - t0) / (t2 - t0) * a2;
        Vector3 b2 = (t3 - u) / (t3 - t1) * a2 + (u - t1) / (t3 - t1) * a3;
        return (t2 - u) / (t2 - t1) * b1 + (u - t1) / (t2 - t1) * b2;
    }

    // Scene 뷰에서 길 모양 미리보기
    private void OnDrawGizmos()
    {
        if (pointsRoot == null) return;

        List<Vector3> keys = new List<Vector3>();
        foreach (Transform child in pointsRoot)
            keys.Add(child.position);
        List<Vector3> curve = SampleCurve(keys);

        Gizmos.color = Color.yellow;
        for (int i = 1; i < curve.Count; i++)
            Gizmos.DrawLine(curve[i - 1], curve[i]);
    }
}
