using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 보스 처치 연출. BossController2.PlayDeath()에서 보스 오브젝트에 붙여 실행됨.
//   1) 히트스톱 + 화면 번쩍 + 단말마
//   2) 슬로모 속 연쇄 폭발 (몸이 붉게 깜빡임)
//   3) 대폭발 + 연기 + 몸 페이드아웃
//   4) 잠깐 정적 후 종료 → UIManager가 IsPlaying이 꺼질 때까지 결과/보상 화면을 미룸
// 연출 타이밍은 실제 시간 기준이고, 일시정지(GamePause) 중에는 진행을 멈춤.
public class BossDeathSequence : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { IsPlaying = false; }

    private BossDeathFxSettings settings;
    private SpriteRenderer[] renderers;
    private Color[] startColors;
    private Vector3 bodyCenter;
    private Vector3 bodyExtents;
    private float bodyHeight;
    private int fxSortingLayerID;
    private int fxSortingOrder;

    private bool ownsTimeScale;
    private GameObject overlayRoot;
    private Image flashImage;
    private Coroutine flashRoutine;

    public static void Play(GameObject boss, SpriteRenderer[] bodyRenderers, Bounds body, BossDeathFxSettings fx)
    {
        if (boss == null || boss.GetComponent<BossDeathSequence>() != null) return;
        BossDeathSequence seq = boss.AddComponent<BossDeathSequence>();
        seq.Begin(bodyRenderers, body, fx != null ? fx : BossDeathFxSettings.Load());
    }

    private void Begin(SpriteRenderer[] bodyRenderers, Bounds body, BossDeathFxSettings fx)
    {
        settings = fx;
        renderers = bodyRenderers ?? new SpriteRenderer[0];
        startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        bodyCenter = body.center;
        bodyExtents = body.extents;
        bodyHeight = Mathf.Max(0.5f, body.size.y);

        // 이펙트는 몸에서 가장 위에 그려지는 조각보다 위로
        int topOrder = 0;
        bool found = false;
        foreach (SpriteRenderer r in renderers)
        {
            if (r == null) continue;
            if (!found || r.sortingOrder > topOrder)
            {
                topOrder = r.sortingOrder;
                fxSortingLayerID = r.sortingLayerID;
                found = true;
            }
        }
        fxSortingOrder = topOrder + 5;

        IsPlaying = true;
        CreateOverlay();
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // ---------- 1) 히트스톱 ----------
        SetTimeScale(settings.hitStopTimeScale);
        Flash(settings.screenFlashColor, settings.screenFlashDuration);
        Shake(settings.hitShake);
        PlaySfx(settings.deathCrySound, false);
        yield return WaitReal(settings.hitStopDuration);

        // ---------- 2) 슬로모 + 연쇄 폭발 ----------
        while (GamePause.IsPaused) yield return null;
        SetTimeScale(settings.slowMoTimeScale);
        AudioClip chainClip = settings.chainExplosionSound != null
            ? settings.chainExplosionSound
            : (SoundManager.Instance != null ? SoundManager.Instance.enemyHitSound : null);

        for (int i = 0; i < settings.chainCount; i++)
        {
            Vector3 p = bodyCenter + new Vector3(
                Random.Range(-bodyExtents.x, bodyExtents.x) * 0.7f,
                Random.Range(-bodyExtents.y, bodyExtents.y) * 0.7f,
                0f);
            SpawnFx(settings.chainExplosionPrefab, p, bodyHeight * settings.chainExplosionSize, i);
            PlaySfx(chainClip, true);
            Shake(settings.chainShake);
            SetBodyTint(i % 2 == 0 ? settings.chainTint : Color.white, 1f);
            yield return WaitReal(settings.chainInterval);
        }

        // ---------- 3) 대폭발 + 소멸 ----------
        // 일시정지 중에 바꾸면 GamePause가 해제할 때 슬로모 값으로 되돌리므로, 풀린 뒤에 복구
        while (GamePause.IsPaused) yield return null;
        SetTimeScale(1f);

        SpawnFx(settings.finalExplosionPrefab, bodyCenter, bodyHeight * settings.finalExplosionSize, 20);
        SpawnFx(settings.finalSmokePrefab, bodyCenter, bodyHeight * settings.finalSmokeSize, 19);
        PlaySfx(settings.finalExplosionSound, false);
        Flash(settings.screenFlashColor, settings.screenFlashDuration * 1.3f);
        Shake(settings.finalShake);

        float t = 0f;
        float fade = Mathf.Max(0.01f, settings.bodyFadeDuration);
        while (t < fade)
        {
            if (!GamePause.IsPaused) t += Time.unscaledDeltaTime;
            SetBodyTint(Color.white, 1f - Mathf.Clamp01(t / fade));
            yield return null;
        }
        foreach (SpriteRenderer r in renderers)
            if (r != null) r.enabled = false;

        // ---------- 4) 정적 ----------
        yield return WaitReal(settings.endDelay);

        Finish();
    }

    // ===================== 시간 =====================

    private IEnumerator WaitReal(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (!GamePause.IsPaused) t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // 일시정지 중에는 GamePause가 timeScale을 관리하므로 건드리지 않음
    private void SetTimeScale(float scale)
    {
        if (GamePause.IsPaused) return;
        Time.timeScale = scale;
        ownsTimeScale = !Mathf.Approximately(scale, 1f);
    }

    private void RestoreTimeScale()
    {
        if (!ownsTimeScale) return;
        if (!GamePause.IsPaused) Time.timeScale = 1f;
        ownsTimeScale = false;
    }

    // ===================== 이펙트 / 사운드 =====================

    private void SpawnFx(GameObject prefab, Vector3 pos, float worldSize, int orderOffset)
    {
        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);

        // 원본 크기를 기준으로 몸 크기에 맞춰 확대/축소
        float native = 0f;
        foreach (ParticleSystem ps in systems)
        {
            native = Mathf.Max(native, ps.main.startSize.constantMax); // Constant 모드면 constant와 같은 값
        }
        if (native > 0.0001f && worldSize > 0f)
            fx.transform.localScale = Vector3.one * (worldSize / native);

        float life = 0.5f;
        foreach (ParticleSystem ps in systems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
            if (pr != null)
            {
                pr.sortingLayerID = fxSortingLayerID;
                pr.sortingOrder = fxSortingOrder + orderOffset;
            }

            life = Mathf.Max(life, main.duration + main.startLifetime.constantMax);
        }
        foreach (ParticleSystem ps in systems) ps.Play(false);

        // 보스 오브젝트와 별개로 남아 있다가 스스로 정리
        Destroy(fx, life + 0.5f);
    }

    private void PlaySfx(AudioClip clip, bool randomPitch)
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(clip, randomPitch);
    }

    private void SetBodyTint(Color tint, float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = startColors[i] * tint;
            c.a = startColors[i].a * alpha;
            renderers[i].color = c;
        }
    }

    // ===================== 화면 흔들림 =====================

    // 실제 흔들기는 CameraShaker가 담당 (타격 흔들림과 카메라 위치가 꼬이지 않도록 공용 처리)
    private void Shake(float amplitude)
    {
        CameraShaker.Shake(amplitude, settings.shakeDuration);
    }

    // ===================== 화면 번쩍 / 입력 차단 =====================

    private void CreateOverlay()
    {
        overlayRoot = new GameObject("BossDeathOverlay");
        Canvas canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        if (settings.blockInput)
        {
            overlayRoot.AddComponent<GraphicRaycaster>();
            Image blocker = CreateFullScreenImage("InputBlocker", new Color(0f, 0f, 0f, 0f));
            blocker.raycastTarget = true;
        }

        flashImage = CreateFullScreenImage("Flash", new Color(1f, 1f, 1f, 0f));
        flashImage.raycastTarget = false;
    }

    private Image CreateFullScreenImage(string objName, Color color)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(overlayRoot.transform, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private void Flash(Color color, float duration)
    {
        if (flashImage == null || duration <= 0f || color.a <= 0f) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (!GamePause.IsPaused) t += Time.unscaledDeltaTime;
            Color c = color;
            c.a = color.a * (1f - Mathf.Clamp01(t / duration));
            flashImage.color = c;
            yield return null;
        }
        flashImage.color = new Color(color.r, color.g, color.b, 0f);
        flashRoutine = null;
    }

    // ===================== 정리 =====================

    private void Finish()
    {
        RestoreTimeScale();
        CameraShaker.Stop();
        if (overlayRoot != null) Destroy(overlayRoot);
        IsPlaying = false;
    }

    // 씬 전환 등으로 중간에 끊겨도 시간/카메라/결과 대기가 꼬이지 않게
    private void OnDestroy()
    {
        if (!IsPlaying) return;
        Finish();
    }
}
