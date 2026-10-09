using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 3D 주사위 굴리기 연출. Dice3D 프리팹(Tools > Dice > 3D 주사위 프리팹 만들기)에 붙어 있다.
// 프리팹은 화면 밖 먼 곳에 놓여 있고, 전용 카메라가 RenderTexture로 찍은 것을 주사위 UI 자리의 RawImage(view)가 보여준다
// (전투 UI는 오버레이 캔버스라 3D 오브젝트를 그냥 두면 UI에 가려짐).
// 결과는 DiceManager가 시드로 먼저 정하고, 이 연출은 그 면이 위로 오도록 착지만 한다 (물리 엔진을 쓰지 않음).
public class Dice3DRoller : MonoBehaviour
{
    [SerializeField] private Camera stageCamera;
    [SerializeField] private Transform die;
    [SerializeField] private Renderer shadow;
    [SerializeField] private RawImage view;
    // 눈 1~6이 그려진 면 (순서 = 눈 - 1). 면이 주사위 중심에서 어느 방향에 있는지로 결과 면을 위로 돌린다
    [SerializeField] private Transform[] valueFaces = new Transform[6];
    // 카메라 쪽을 볼 때만 그리고 밝기를 바꿀 면들 (면 그림 + 그 뒤의 몸통)
    [SerializeField] private Renderer[] faceRenderers;

    [Header("던지기 (화면 왼쪽 밖에서 포물선으로 날아와 튕기며 멈춤)")]
    [SerializeField] private float restHeight = 0.5f; // 바닥에 놓였을 때 중심 높이 (주사위 크기 1)
    [Tooltip("멈출 곳 기준 출발 위치 (화면 밖). y = 바닥에서 떠 있는 높이")]
    [SerializeField] private Vector3 launchOffset = new Vector3(-10.5f, 1.5f, 1.2f);
    [SerializeField] private float flightDuration = 0.6f;
    [Tooltip("날아오는 포물선이 직선보다 얼마나 높이 솟는지")]
    [SerializeField] private float flightArcHeight = 3f;
    [Tooltip("첫 착지까지 이동하는 거리 비율 (나머지는 튕기면서 이동)")]
    [SerializeField, Range(0.1f, 1f)] private float firstLandingProgress = 0.75f;
    // 첫 착지 후 튕김: 시간 / 높이 / 이동 거리 비율 (같은 순서)
    [SerializeField] private float[] bounceTimes = { 0.32f, 0.22f, 0.14f };
    [SerializeField] private float[] bounceArcHeights = { 0.9f, 0.35f, 0.1f };
    [SerializeField] private float[] bounceDistances = { 0.7f, 0.22f, 0.08f };
    [SerializeField] private float tumbleDegrees = 1080f;

    [Header("착지 연출")]
    [SerializeField] private float punchScale = 1.25f;
    [SerializeField] private float punchDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.1f; // 카메라가 멀어서 예전보다 조금 크게
    [SerializeField] private float shakeDuration = 0.1f;

    [Header("면 밝기 (조명 없이 면 방향으로 계산)")]
    [SerializeField] private Vector3 lightDirection = new Vector3(0.3f, 1f, -0.5f);
    [SerializeField, Range(0f, 1f)] private float minBrightness = 0.55f;

    private Material[] faceMaterials;
    private Color[] faceBaseColors;
    private Material shadowMaterial;
    private Color shadowBaseColor;
    private Vector3 cameraHome;
    private float shakeTimer;

    private void Awake()
    {
        // 머티리얼 에셋을 직접 바꾸지 않도록 인스턴스로 복사해 씀
        faceMaterials = new Material[faceRenderers.Length];
        faceBaseColors = new Color[faceRenderers.Length];
        for (int i = 0; i < faceRenderers.Length; i++)
        {
            faceMaterials[i] = faceRenderers[i].material;
            faceBaseColors[i] = faceMaterials[i].color;
        }

        shadowMaterial = shadow.material;
        shadowBaseColor = shadowMaterial.color;
        cameraHome = stageCamera.transform.localPosition;

        Hide();
    }

    // value(1~6)가 위로 오도록 던져서 착지시킨다. onFirstLanding: 날아온 주사위가 처음 바닥에 닿는 순간 호출 (효과음 등)
    public IEnumerator Roll(int value, System.Action onFirstLanding = null)
    {
        value = Mathf.Clamp(value, 1, 6);
        Show();

        // 결과 면이 위로 + 매번 다른 방향으로 놓이도록 90도 단위 회전 (보이는 연출용이라 시드와 무관)
        Vector3 resultNormal = valueFaces[value - 1].localPosition.normalized;
        Quaternion target = Quaternion.AngleAxis(90f * Random.Range(0, 4), Vector3.up) *
                            Quaternion.FromToRotation(resultNormal, Vector3.up);
        Vector3 tumbleAxis = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f), -1f).normalized;

        // 구간 0 = 화면 밖에서 날아오는 포물선, 1~ = 바닥에 닿은 뒤의 튕김.
        // 구간마다 시간 / 이동 거리 비율(출발 0 → 멈춤 1)을 미리 계산 (구간 안에서는 일정한 속도로 이동)
        int segmentCount = bounceTimes.Length + 1;
        float[] durations = new float[segmentCount];
        float[] pathEnd = new float[segmentCount];
        durations[0] = flightDuration;
        pathEnd[0] = firstLandingProgress;

        float distanceSum = 0f;
        for (int i = 0; i < bounceTimes.Length; i++) distanceSum += BounceDistance(i);

        float totalDuration = flightDuration;
        for (int i = 0; i < bounceTimes.Length; i++)
        {
            durations[i + 1] = bounceTimes[i];
            pathEnd[i + 1] = pathEnd[i] + (1f - firstLandingProgress) * (distanceSum > 0f ? BounceDistance(i) / distanceSum : 0f);
            totalDuration += bounceTimes[i];
        }
        pathEnd[segmentCount - 1] = 1f;

        Vector3 restPosition = Vector3.up * restHeight;
        Vector3 launchHorizontal = new Vector3(launchOffset.x, 0f, launchOffset.z);

        float elapsed = 0f;
        int segment = 0;
        float segmentStart = 0f;
        die.localScale = Vector3.one;

        while (elapsed < totalDuration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, totalDuration);

            // 바닥에 닿아 다음 튕김으로 넘어갈 때 화면을 살짝 흔듦
            while (segment < segmentCount - 1 && elapsed >= segmentStart + durations[segment])
            {
                segmentStart += durations[segment];
                segment++;
                shakeTimer = shakeDuration;
                if (segment == 1) onFirstLanding?.Invoke();
            }

            float s = Mathf.Clamp01((elapsed - segmentStart) / Mathf.Max(durations[segment], 0.0001f));
            float pathStart = segment == 0 ? 0f : pathEnd[segment - 1];

            // 바닥에 놓였을 때보다 얼마나 떠 있는지
            float height = segment == 0
                ? launchOffset.y * (1f - s) + 4f * flightArcHeight * s * (1f - s)   // 출발 높이에서 포물선으로 날아와 착지
                : 4f * BounceHeight(segment - 1) * s * (1f - s);                     // 포물선으로 튕김

            Vector3 horizontal = Vector3.Lerp(launchHorizontal, Vector3.zero, Mathf.Lerp(pathStart, pathEnd[segment], s));
            die.localPosition = restPosition + horizontal + Vector3.up * height;

            // 공중에서 빠르게 돌다가 튕길수록 느려지며 결과 면으로 맞춰짐
            float spin = EaseOutCubic(elapsed / totalDuration);
            die.localRotation = Quaternion.AngleAxis(tumbleDegrees * (1f - spin), tumbleAxis) * target;

            UpdateVisuals();
            yield return null;
        }

        die.localPosition = restPosition;
        die.localRotation = target;
        shakeTimer = shakeDuration;

        // 착지 강조: 살짝 커졌다가 돌아옴
        for (float t = 0f; t < punchDuration; t += Time.deltaTime)
        {
            die.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, Mathf.Sin(t / punchDuration * Mathf.PI));
            UpdateVisuals();
            yield return null;
        }

        die.localScale = Vector3.one;
        UpdateVisuals();
    }

    public void Hide()
    {
        if (view != null) view.gameObject.SetActive(false);
        stageCamera.enabled = false;
    }

    private void Show()
    {
        if (view != null) view.gameObject.SetActive(true);
        stageCamera.enabled = true;
    }

    private void Update()
    {
        if (shakeTimer <= 0f) return;

        shakeTimer -= Time.deltaTime;
        float strength = shakeStrength * Mathf.Clamp01(shakeTimer / shakeDuration);
        stageCamera.transform.localPosition = cameraHome + (Vector3)(Random.insideUnitCircle * strength);
        if (shakeTimer <= 0f) stageCamera.transform.localPosition = cameraHome;
    }

    // 카메라 쪽을 보는 면만 그린다. 면 셰이더(UI 기본)가 깊이를 쓰지 않지만, 볼록한 정육면체는 앞면끼리 겹치지 않아 올바르게 보임.
    // 면 밝기는 빛 방향과의 각도로, 그림자는 높이에 따라 크기 / 진하기를 바꾼다.
    private void UpdateVisuals()
    {
        Vector3 cameraPosition = stageCamera.transform.position;
        Vector3 light = lightDirection.normalized;

        for (int i = 0; i < faceRenderers.Length; i++)
        {
            Transform face = faceRenderers[i].transform;
            Vector3 normal = (face.position - die.position).normalized;
            bool visible = Vector3.Dot(normal, cameraPosition - face.position) > 0f;

            faceRenderers[i].enabled = visible;
            if (!visible) continue;

            Color c = faceBaseColors[i] * Mathf.Lerp(minBrightness, 1f, Mathf.Max(0f, Vector3.Dot(normal, light)));
            c.a = faceBaseColors[i].a;
            faceMaterials[i].color = c;
        }

        float height01 = Mathf.Clamp01((die.localPosition.y - restHeight) / (launchOffset.y + flightArcHeight));
        Transform shadowTransform = shadow.transform;
        shadowTransform.localPosition = new Vector3(die.localPosition.x, shadowTransform.localPosition.y, die.localPosition.z);
        shadowTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 0.8f, height01) * die.localScale.x;

        Color shadowColor = shadowBaseColor;
        shadowColor.a *= Mathf.Lerp(1f, 0.35f, height01);
        shadowMaterial.color = shadowColor;
    }

    // 배열 길이가 서로 달라도 빠진 값은 0으로 처리
    private float BounceHeight(int i) => i < bounceArcHeights.Length ? bounceArcHeights[i] : 0f;
    private float BounceDistance(int i) => i < bounceDistances.Length ? bounceDistances[i] : 0f;

    private static float EaseOutCubic(float t)
    {
        t = 1f - Mathf.Clamp01(t);
        return 1f - t * t * t;
    }

    private void OnDestroy()
    {
        if (faceMaterials != null)
            foreach (Material m in faceMaterials) Destroy(m);
        if (shadowMaterial != null) Destroy(shadowMaterial);
    }
}
