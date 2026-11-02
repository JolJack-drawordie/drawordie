using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// Animator와 애니메이션 클립이 들어 있는 일반 몬스터(DungeonMonsters2D 에셋)용 공통 컨트롤러.
// 몬스터마다 스크립트를 따로 만들지 않고, 프리팹 인스펙터에서 Animator 상태 이름과 모션 값만 맞춰 재사용한다.
// - 공격: 코드로 플레이어 앞까지 돌진하면서 몬스터 고유 Attack 클립을 함께 재생
// - 방어: Defend 클립이 있으면 재생, 없으면 몸이 부풀었다 줄어드는 펄스 + 파란 틴트
// - 피격: Stuned 클립 앞부분을 짧게 재생, 사망: Death 클립 마지막 프레임에서 멈춤
public class AnimatedEnemyController : EnemyController
{
    // 몬스터 성격에 맞춘 모션 값 묶음 (Custom이면 인스펙터 숫자를 그대로 사용)
    public enum MotionPreset { Custom, Zombie, Vampire, Succubus }

    private Animator animator;
    private int baseAttackPower; // EnemyUnit의 기본 공격력 (설정 누락 진단용)
    private SpriteRenderer[] allRenderers; // 리깅 캐릭터는 몸 조각마다 SpriteRenderer가 따로 있음
    private Color[] baseColors; // 몸 조각별 원래 색 (틴트 후 복원용)
    private Coroutine tintCoroutine;
    // 펄스 / 돌진이 끝난 뒤 돌아올 기준 크기 / 위치 / 회전
    private Vector3 pulseBaseScale;
    private Vector3 pulseBasePos;
    private Quaternion pulseBaseRot;
    private Quaternion canvasBaseRot; // 몬스터가 기울어져도 머리 위 아이콘은 똑바로 유지하기 위한 기준 회전

    [Header("스폰 위치 보정 (스폰 포인트 기준으로 이동)")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("의도 아이콘 위치 (몬스터 루트 위치 기준으로 머리 위에 띄울 거리)")]
    public Vector3 intentCanvasOffset = new Vector3(0f, 2f, 0f);
    [Tooltip("켜면 몸의 Head 조각(없으면 몸 전체) 꼭대기 바로 위에 아이콘을 자동으로 띄움. 끄면 위의 Intent Canvas Offset 사용")]
    public bool autoIconAboveHead = false;
    public float iconGapAboveHead = 0.4f; // 머리 꼭대기와 아이콘 사이 간격
    public string headPartName = "Head";
    public string[] iconIgnoreKeywords = { "Weapon", "Effect", "Fire", "Smoke", "Spell", "Projectile", "Stone", "Arrow" }; // 아이콘 위치 / 몸 범위를 잴 때 빼는 조각 이름 (무기, 이펙트 등)
    private Vector3 autoIconOffset;
    private bool hasAutoIconOffset;
    public int intentCanvasSortingBoost = 10; // 아이콘을 몸 조각 중 가장 높은 정렬 순서보다 얼마나 더 위에 그릴지

    [Header("Animator 상태 이름 (Animator 창의 상태 이름과 같아야 함)")]
    public string stateSuffix = ""; // 상태 이름 끝에 이름이 붙어 있으면(IdleDragon 등) 그 부분 입력 (예: "Dragon")
    public string idleState = "Idle";
    public string attackState = "Attack";
    public string defendState = ""; // 비우면 애니메이션 없이 펄스만
    public string hurtState = "Stuned"; // 비우면 피격 애니메이션 없이 반짝임만
    public string deathState = "Death"; // 비우면 사망 애니메이션 없음
    [Tooltip("피격 모션을 이 시간(초)만 재생하고 Idle로 돌아감. Stuned 클립은 2~3초짜리 기절 모션이라 앞부분만 짧게 씀. 0 이하면 클립 끝까지 재생")]
    public float hurtMaxDuration = 0.4f;

    [Header("방어 연출 (방어 애니메이션이 없어도 색으로 방어를 표시)")]
    public Color guardTintColor = new Color(0.55f, 0.75f, 1f, 1f);
    public float guardTintDuration = 0.5f; // 0이면 사용 안 함

    [Header("몬스터별 모션 프리셋 (공격 모션 + 방어 펄스의 추천 값을 한 번에 적용)")]
    [FormerlySerializedAs("pulsePreset")] // 예전 필드 이름에 연결돼 있던 값을 그대로 이어받음
    public MotionPreset motionPreset = MotionPreset.Custom;

    [Header("공격 모션 (코드) - Use Code Attack이 켜져 있으면 위치 이동(돌진)을 코드로 재생")]
    public bool useCodeAttack = true;
    public bool playAttackClip = true; // 돌진과 함께 몬스터 고유 Attack 클립도 재생 (몬스터마다 동작이 달라 보이게)
    public float attackDistance = 10f; // 플레이어 쪽(왼쪽)으로 돌진할 거리 (Dash To Player가 꺼져 있거나 계산에 실패했을 때)
    [Tooltip("켜면 Attack Distance 대신 플레이어 바로 앞까지 거리를 자동 계산해서 돌진 (활을 쏘는 원거리 몬스터는 끄기)")]
    public bool dashToPlayer = true;
    public float attackStopGap = 0.3f; // 플레이어 몸 앞에서 멈출 간격 (클수록 덜 붙음)
    public float maxDashDistance = 15f; // 자동 계산 거리의 최대값 (안전장치)
    public float attackWindupBack = 0.2f; // 돌진 전에 뒤(오른쪽)로 물러나는 거리 (0이면 생략)
    public float attackWindupTime = 0.2f;
    public float attackDashTime = 0.2f;
    public float attackStrikeTime = 0.15f; // 타격 순간 머무는 시간
    public float attackReturnTime = 0.3f;
    public float attackArcHeight = 0f; // 돌진 중 위로 뜨는 높이 (곡선 이동)
    public float attackStrikeShake = 0.1f; // 타격 순간 떨림 세기
    public float attackLean = 0f; // 돌진 중 앞으로 기울어지는 각도(도)
    public float attackStrikeScale = 1f; // 타격 순간 크기 배율 (1이면 변화 없음)
    public bool waitAttackClipBeforeReturn = true; // Attack 클립이 끝날 때까지 그 자리에서 기다렸다가 복귀
    [Range(0.3f, 1f)] public float attackClipEndRatio = 0.9f; // 클립이 이 비율까지 재생되면 복귀 시작
    public float attackReturnDelay = 0.15f; // 복귀 전에 잠깐 머무는 시간

    [Header("방어 펄스 (Defend State가 비어 있을 때 코드로 재생되는 방어 동작)")]
    public float pulseDuration = 0.6f; // 전체 시간 (0이면 펄스 없이 대기만)
    public int pulseCount = 1; // 부풀었다 줄어드는 횟수
    public float pulseScale = 1.1f; // 가장 커졌을 때의 크기 배율
    public float pulseSquash = 0f; // + 가로로 넓고 낮게 / - 세로로 길고 좁게
    public float pulseRise = 0f; // 위로 뜨는 높이
    public float pulseShake = 0f; // 좌우 떨림 세기
    public float pulseSway = 0f; // 좌우로 기울어지는 각도(도)

    [Header("재생 설정")]
    public float crossFadeTime = 0.05f;
    public float maxActionTime = 4f; // 애니메이션이 안 끝나는 경우를 대비한 안전장치
    public float fallbackDuration = 0.6f; // 재생할 상태가 없을 때 기다리는 시간

    protected override void Start()
    {
        // 스폰 위치 보정 (base.Start 전에 적용해야 기준 위치가 맞음)
        transform.position += spawnOffset;

        base.Start(); // EnemyUnit 연결 + 피격 이벤트 구독 + 첫 행동 결정

        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Animator가 여러 개면 클립의 뼈대 경로가 맞지 않는 쪽이 선택돼 몸이 안 움직일 수 있으므로 경고
        Animator[] allAnimators = GetComponentsInChildren<Animator>();
        if (animator != null && allAnimators.Length > 1)
        {
            Debug.LogWarning(
                $"[{name}] Animator가 {allAnimators.Length}개 있습니다. 클립은 뼈대(몸 조각)가 있는 자식 오브젝트의 Animator에서만 " +
                $"동작하니, 루트 등 나머지 Animator는 제거하세요. (현재 사용 중: {animator.gameObject.name})", this);
        }

        if (animator == null)
        {
            Debug.LogError($"[{name}] Animator를 찾지 못했습니다!", this);
        }
        else
        {
            ResolveAllStates();
        }

        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        PrepareBodyRenderers(allRenderers);
        baseColors = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++) baseColors[i] = allRenderers[i].color;

        // 머리 위 아이콘이 몸 조각 뒤로 가려지지 않도록, 몸 조각 중 가장 높은 정렬 순서보다 위에 그림
        if (intentCanvas != null && allRenderers.Length > 0)
        {
            SpriteRenderer top = allRenderers[0];
            for (int i = 1; i < allRenderers.Length; i++)
            {
                if (allRenderers[i].sortingOrder > top.sortingOrder) top = allRenderers[i];
            }

            intentCanvas.sortingLayerID = top.sortingLayerID;
            intentCanvas.sortingOrder = top.sortingOrder + intentCanvasSortingBoost;
        }

        // 펄스 / 돌진이 끝난 뒤 돌아올 기준 상태 (spawnOffset 적용 이후 값)
        pulseBaseScale = transform.localScale;
        pulseBasePos = transform.localPosition;
        pulseBaseRot = transform.localRotation;
        ApplyMotionPreset();
        ComputeAutoIconOffset();

        if (intentCanvas != null) canvasBaseRot = intentCanvas.transform.rotation;

        if (hitUnit != null) baseAttackPower = hitUnit.attackPower;

        // 프리팹 설정 누락 진단 (전투에서는 Resources/Monsters의 프리팹이 쓰임)
        if (hitUnit == null) Debug.LogWarning($"[{name}] EnemyUnit이 없어 체력/공격이 동작하지 않습니다.", this);
        else if (baseAttackPower <= 0) Debug.LogWarning($"[{name}] EnemyUnit의 Attack Power가 {baseAttackPower}입니다. 공격해도 데미지가 들어가지 않아요.", this);

        if (intentCanvas == null) Debug.LogWarning($"[{name}] Intent Canvas가 비어 있어 머리 위 아이콘 위치를 잡지 못합니다.", this);
        if (intentIcon == null) Debug.LogWarning($"[{name}] Intent Icon이 비어 있습니다.", this);
        if (attackSprite == null || defendSprite == null) Debug.LogWarning($"[{name}] Attack Sprite/Defend Sprite가 비어 있어 아이콘 이미지가 바뀌지 않습니다.", this);
    }

    // 몸 조각 범위로 "루트 → 머리 꼭대기 위" 거리를 한 번 계산 (몬스터마다 키가 달라도 아이콘이 머리 위에 오도록)
    private void ComputeAutoIconOffset()
    {
        hasAutoIconOffset = false;
        if (!autoIconAboveHead || allRenderers == null || allRenderers.Length == 0) return;

        // Idle 자세를 먼저 적용해야 숨겨진 이펙트 조각이 빠진 정확한 범위를 잴 수 있음
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.Update(0f);

        bool has = false;
        bool hasHead = false;
        Bounds body = new Bounds(transform.position, Vector3.zero);
        Bounds head = body;

        foreach (SpriteRenderer r in allRenderers)
        {
            if (r == null || r.sprite == null || !r.enabled || !r.gameObject.activeInHierarchy || r.color.a < 0.05f) continue;
            if (IsIconIgnoredPart(r)) continue;

            if (!has) { body = r.bounds; has = true; }
            else body.Encapsulate(r.bounds);

            if (!hasHead && string.Equals(r.gameObject.name, headPartName, System.StringComparison.OrdinalIgnoreCase))
            {
                head = r.bounds;
                hasHead = true;
            }
        }

        if (!has) return;

        float x = hasHead ? head.center.x : body.center.x;
        float top = hasHead ? Mathf.Max(body.max.y, head.max.y) : body.max.y;
        autoIconOffset = new Vector3(x, top + iconGapAboveHead, transform.position.z) - transform.position;
        hasAutoIconOffset = true;
    }

    // 돌진 이동량 (localPosition 기준). 기본은 왼쪽으로 Attack Distance만큼,
    // Dash To Player면 몸 앞(왼쪽 끝)이 플레이어 몸 앞(오른쪽 끝) + Attack Stop Gap 위치에 닿도록 자동 계산
    private Vector3 GetDashOffset()
    {
        Vector3 fixedOffset = new Vector3(-attackDistance, 0f, 0f);
        if (!dashToPlayer || !TryGetDashDistanceToPlayer(out float worldDistance)) return fixedOffset;

        // 거리는 월드 단위로 계산했으므로, 부모가 있으면 부모 크기를 반영해 localPosition 단위로 변환
        Vector3 worldOffset = new Vector3(-worldDistance, 0f, 0f);
        return transform.parent != null ? transform.parent.InverseTransformVector(worldOffset) : worldOffset;
    }

    // 행동 시작 시점(제자리)의 몸 범위로 돌진 거리 계산 (보스 BossController2와 같은 방식)
    private bool TryGetDashDistanceToPlayer(out float distance)
    {
        distance = 0f;

        // 플레이어는 칼 / 이펙트를 뺀 몸통 앞(PlayerBodyBounds), 몬스터는 무기 / 이펙트를 뺀 몸 앞
        if (!PlayerBodyBounds.TryGetFrontX(out float playerFront) || !TryGetBodyBounds(out Bounds body)) return false;

        float d = body.min.x - playerFront - attackStopGap;
        if (d <= 0f) return false;

        distance = Mathf.Min(d, maxDashDistance);
        return true;
    }

    // 실제로 보이는 몸 조각들의 범위 (무기 / 이펙트처럼 iconIgnoreKeywords에 걸리는 조각과 숨겨진 조각은 제외)
    private bool TryGetBodyBounds(out Bounds bounds)
    {
        bounds = new Bounds(transform.position, Vector3.zero);
        if (allRenderers == null) return false;

        bool has = false;
        for (int pass = 0; pass < 2 && !has; pass++)
        {
            foreach (SpriteRenderer r in allRenderers)
            {
                if (r == null || r.sprite == null || !r.enabled || !r.gameObject.activeInHierarchy || r.color.a < 0.05f) continue;
                if (pass == 0 && IsIconIgnoredPart(r)) continue; // 몸 조각을 못 찾으면 두 번째 패스에서 전부 포함

                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
        }
        return has;
    }

    // 이름에 무기 / 이펙트 키워드가 들어간 조각인지
    private bool IsIconIgnoredPart(SpriteRenderer r)
    {
        if (iconIgnoreKeywords == null) return false;
        foreach (string k in iconIgnoreKeywords)
        {
            if (!string.IsNullOrEmpty(k) && r.gameObject.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    // 아이콘 이미지의 중심이 (몬스터 위치 + 오프셋)에 오도록 캔버스를 옮긴다 (캔버스 크기 / 피벗과 상관없이 정확히 머리 위)
    private void PlaceIntentCanvas()
    {
        Vector3 target = transform.position + (hasAutoIconOffset ? autoIconOffset : intentCanvasOffset);

        if (intentIcon != null)
        {
            RectTransform iconRect = intentIcon.rectTransform;
            Vector3 iconCenter = iconRect.TransformPoint(iconRect.rect.center);
            intentCanvas.transform.position += target - iconCenter;
        }
        else
        {
            intentCanvas.transform.position = target;
        }
    }

    // 애니메이션으로 움직이는 몬스터라 부모의 숨쉬기(크기 변화) 연출은 쓰지 않음
    protected override void Update()
    {
        if (intentCanvas != null)
        {
            intentCanvas.transform.rotation = canvasBaseRot;
            PlaceIntentCanvas();
        }
    }

    protected override IEnumerator PlayCustomAttack()
    {
        if (useCodeAttack)
        {
            // 돌진 / 타격은 코드로, 몬스터 고유 Attack 클립도 같이 틀어서 몬스터마다 다른 동작(할퀴기 / 물기 등)이 보이게 함
            if (playAttackClip && HasState(attackState))
                animator.CrossFadeInFixedTime(attackState, crossFadeTime);

            yield return StartCoroutine(AttackMotionRoutine());

            // Attack 클립이 Idle로 자동 복귀하지 않는 경우를 대비해 직접 되돌림
            if (playAttackClip && !hitIsDead) ReturnToIdle();
        }
        else
        // 코드 돌진 없이 Attack 클립만 재생
        {
            PlayAttackSound();
            NotifyAttackImpact(currentAttack);
            yield return StartCoroutine(PlayAndWait(attackState, fallbackDuration));
            if (!hitIsDead) ReturnToIdle();
        }
    }

    protected override IEnumerator PlayCustomDefend()
    {
        // 방어 중임을 알리도록 몸 색을 잠깐 파랗게
        if (guardTintDuration > 0f) StartTint(guardTintColor, guardTintDuration);

        if (HasState(defendState))
        {
            yield return StartCoroutine(PlayAndWait(defendState, fallbackDuration));

            if (!hitIsDead) ReturnToIdle();
            PlayDefendSound();
        }
        else if (pulseDuration > 0f)
        {
            // 방어 클립이 없으면 코드로 만든 펄스
            yield return StartCoroutine(PulseRoutine());

            PlayDefendSound();
        }
        else
        {
            yield return new WaitForSeconds(fallbackDuration);
        }
    }

    // 방어 펄스: 몸 전체가 부풀었다 줄어드는 동작을 pulseCount번 반복 (뼈대 애니메이션은 그대로 재생됨)
    protected virtual IEnumerator PulseRoutine()
    {
        int count = Mathf.Max(1, pulseCount);
        float t = 0f;

        while (t < pulseDuration && !hitIsDead)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / pulseDuration);

            float w = Mathf.Sin(p * Mathf.PI * count);
            float k = w * w; // 0 → 1 → 0을 count번 부드럽게 반복

            float grow = (pulseScale - 1f) * k;
            float squash = pulseSquash * k;
            transform.localScale = new Vector3(
                pulseBaseScale.x * (1f + grow + squash),
                pulseBaseScale.y * (1f + grow - squash),
                pulseBaseScale.z);

            float shakeX = pulseShake > 0f ? Mathf.Sin(t * 60f) * pulseShake * k : 0f;
            transform.localPosition = pulseBasePos + new Vector3(shakeX, pulseRise * k, 0f);

            float tilt = Mathf.Sin(p * Mathf.PI * 2f * count) * pulseSway * k;
            transform.localRotation = pulseBaseRot * Quaternion.Euler(0f, 0f, tilt);

            yield return null;
        }

        // 원래 상태로 복원
        transform.localScale = pulseBaseScale;
        transform.localPosition = pulseBasePos;
        transform.localRotation = pulseBaseRot;
    }

    // 공격 모션: 뒤로 물러났다가(준비) → 플레이어 앞으로 돌진 → 타격(떨림) → 제자리로 복귀
    protected virtual IEnumerator AttackMotionRoutine()
    {
        Vector3 start = pulseBasePos;
        Vector3 target = start + GetDashOffset();

        Vector3 from = start;
        float tiltFrom = 0f;

        // 1) 준비 동작 (뒤로 물러나며 살짝 젖혀짐)
        if (attackWindupBack > 0f && attackWindupTime > 0f)
        {
            Vector3 back = start + new Vector3(attackWindupBack, 0f, 0f);
            float backTilt = -attackLean * 0.5f;
            yield return StartCoroutine(MoveOverTime(start, back, attackWindupTime, 0f, 0f, backTilt));
            from = back;
            tiltFrom = backTilt;
        }

        // 2) 돌진 (앞으로 기울어지며, arcHeight가 있으면 곡선을 그리며 이동)
        yield return StartCoroutine(MoveOverTime(from, target, attackDashTime, attackArcHeight, tiltFrom, attackLean));

        // 3) 타격 (제자리 떨림 + 크기 펀치). 이 순간 효과음과 화면 흔들림
        if (!hitIsDead)
        {
            PlayAttackSound();
            NotifyAttackImpact(currentAttack);
        }
        float t = 0f;
        while (t < attackStrikeTime && !hitIsDead)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / attackStrikeTime);

            Vector3 shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * attackStrikeShake;
            transform.localPosition = target + shake;

            float s = Mathf.Lerp(1f, attackStrikeScale, Mathf.Sin(p * Mathf.PI));
            transform.localScale = new Vector3(pulseBaseScale.x * s, pulseBaseScale.y * s, pulseBaseScale.z);

            yield return null;
        }

        transform.localPosition = target;
        transform.localScale = pulseBaseScale;

        // Attack 클립을 끝까지 보여 준 뒤 잠깐 머물렀다가 복귀 (휘두르는 도중에 뒤로 미끄러지지 않도록)
        if (waitAttackClipBeforeReturn && playAttackClip && HasState(attackState))
        {
            float timer = 0f;
            while (timer < maxActionTime && !hitIsDead)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (!info.IsName(attackState) || info.normalizedTime >= attackClipEndRatio) break;

                timer += Time.deltaTime;
                yield return null;
            }
        }
        if (attackReturnDelay > 0f && !hitIsDead) yield return new WaitForSeconds(attackReturnDelay);

        // 4) 복귀
        yield return StartCoroutine(MoveOverTime(target, start, attackReturnTime, 0f, attackLean, 0f));

        // 원래 상태로 복원
        transform.localScale = pulseBaseScale;
        transform.localPosition = start;
        transform.localRotation = pulseBaseRot;
    }

    // from → to로 duration 동안 부드럽게 이동 (arcHeight: 중간에 위로 솟는 높이, tilt: z축 기울기)
    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration, float arcHeight, float tiltFrom, float tiltTo)
    {
        float t = 0f;

        while (t < duration && !hitIsDead)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float e = p * p * (3f - 2f * p); // smoothstep

            Vector3 pos = Vector3.Lerp(from, to, e);
            pos.y += Mathf.Sin(p * Mathf.PI) * arcHeight;
            transform.localPosition = pos;

            float tilt = Mathf.Lerp(tiltFrom, tiltTo, e);
            transform.localRotation = pulseBaseRot * Quaternion.Euler(0f, 0f, tilt);

            yield return null;
        }

        transform.localPosition = to;
        transform.localRotation = pulseBaseRot * Quaternion.Euler(0f, 0f, tiltTo);
    }

    // 몬스터 성격에 맞춘 추천 모션 값 (공격 + 방어 펄스)
    private void ApplyMotionPreset()
    {
        switch (motionPreset)
        {
            case MotionPreset.Zombie:
                // 묵직하고 느림: 크게 뒤로 젖혔다가 비틀거리며 돌진, 쿵 하고 내려찍고 천천히 복귀
                useCodeAttack = true;
                attackDistance = 3.5f; attackWindupBack = 0.4f; attackWindupTime = 0.35f;
                attackDashTime = 0.35f; attackStrikeTime = 0.25f; attackReturnTime = 0.5f;
                attackArcHeight = 0f; attackStrikeShake = 0.06f; attackLean = 8f; attackStrikeScale = 1.08f;
                // 방어: 웅크리며 넓어지고 비틀거림
                pulseDuration = 0.8f; pulseCount = 1; pulseScale = 1.10f;
                pulseSquash = 0.07f; pulseRise = 0f; pulseShake = 0.04f; pulseSway = 4f;
                break;

            case MotionPreset.Vampire:
                // 빠르고 날렵함: 짧게 물러났다가 위로 솟구치며 순식간에 돌진, 짧고 강한 타격 후 빠르게 복귀
                useCodeAttack = true;
                attackDistance = 4.5f; attackWindupBack = 0.25f; attackWindupTime = 0.12f;
                attackDashTime = 0.14f; attackStrikeTime = 0.12f; attackReturnTime = 0.25f;
                attackArcHeight = 0.9f; attackStrikeShake = 0.12f; attackLean = 12f; attackStrikeScale = 1f;
                // 방어: 위로 솟으며 길쭉해짐, 두 번
                pulseDuration = 0.5f; pulseCount = 2; pulseScale = 1.08f;
                pulseSquash = -0.05f; pulseRise = 0.15f; pulseShake = 0f; pulseSway = 0f;
                break;

            case MotionPreset.Succubus:
                // 유연하고 우아함: 준비 없이 부드러운 곡선으로 다가가 가볍게 타격하고 천천히 복귀
                useCodeAttack = true;
                attackDistance = 4f; attackWindupBack = 0f; attackWindupTime = 0f;
                attackDashTime = 0.35f; attackStrikeTime = 0.15f; attackReturnTime = 0.4f;
                attackArcHeight = 0.5f; attackStrikeShake = 0.03f; attackLean = 5f; attackStrikeScale = 1.05f;
                // 방어: 심장 박동처럼 세 번, 살랑거림
                pulseDuration = 0.7f; pulseCount = 3; pulseScale = 1.07f;
                pulseSquash = 0f; pulseRise = 0.08f; pulseShake = 0f; pulseSway = 6f;
                break;
        }
    }

    // 몸 조각이 여러 개인 캐릭터라, 부모의 반짝임(렌더러 1개) 대신 모든 조각에 색을 입힌다
    protected override void PlayHitEffect()
    {
        StartTint(hitFlashColor, hitFlashDuration);

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position + hitEffectOffset, Quaternion.identity);
        }
    }

    // 모든 몸 조각에 색을 곱했다가(원래 색 × tint) 일정 시간 뒤 원래 색으로 복원
    private void StartTint(Color tint, float duration)
    {
        if (allRenderers == null || allRenderers.Length == 0) return;

        if (tintCoroutine != null) StopCoroutine(tintCoroutine);
        tintCoroutine = StartCoroutine(TintRoutine(tint, duration));
    }

    private IEnumerator TintRoutine(Color tint, float duration)
    {
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null) allRenderers[i].color = baseColors[i] * tint;
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null) allRenderers[i].color = baseColors[i];
        }
    }

    // 부모의 공통 반짝임에 피격 / 사망 애니메이션을 덧붙임
    protected override void HandleHpChanged(int current, int max)
    {
        if (hitIsDead) return;

        bool tookDamage = current < hitLastHp; // base가 hitLastHp를 갱신하기 전에 먼저 판단
        bool willDie = current <= 0;

        base.HandleHpChanged(current, max); // 공통 반짝임 + 사망 여부 + 마지막 체력 갱신

        if (willDie)
        {
            PlayDeath();
        }
        else if (tookDamage && !isActing && HasState(hurtState))
        {
            // 연속으로 맞으면 이전 피격 모션을 끊고 처음부터 다시 (이전 것이 중간에 Idle로 되돌리지 않도록)
            if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
            hurtCoroutine = StartCoroutine(HurtRoutine());
        }
    }

    private Coroutine hurtCoroutine;

    // 피격 모션: hurtMaxDuration초만 재생하고 Idle로 (0 이하면 클립 끝까지)
    private IEnumerator HurtRoutine()
    {
        if (hurtMaxDuration > 0f)
        {
            // 클립 앞부분만 짧게 재생 (매번 첫 프레임부터)
            animator.CrossFadeInFixedTime(hurtState, crossFadeTime, 0, 0f);
            yield return new WaitForSeconds(hurtMaxDuration);
        }
        else
        {
            yield return StartCoroutine(PlayAndWait(hurtState, 0f));
        }

        hurtCoroutine = null;

        // 그 사이에 다른 행동이 시작됐거나 죽었다면 Idle로 되돌리지 않음
        if (!isActing && !hitIsDead) ReturnToIdle();
    }

    // 사망: 이후 어떤 행동도 하지 않게 막고 Death 클립 재생
    private void PlayDeath()
    {
        isActing = true;

        if (intentIcon != null) intentIcon.gameObject.SetActive(false);

        if (HasState(deathState))
        {
            animator.CrossFadeInFixedTime(deathState, crossFadeTime);
            StartCoroutine(FreezeOnDeathEnd());
        }
    }

    // Death 클립이 거의 끝나는 지점에서 멈춰 세움 (Death → Idle 전환이 있어도 다시 일어나지 않도록)
    private IEnumerator FreezeOnDeathEnd()
    {
        float timer = 0f;

        while (timer < maxActionTime)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(deathState) && info.normalizedTime >= 0.95f) break;

            timer += Time.deltaTime;
            yield return null;
        }

        animator.speed = 0f;
    }

    // 상태를 재생하고 끝날 때까지 기다림 (상태가 없으면 fallback 시간만큼 대기)
    private IEnumerator PlayAndWait(string stateName, float fallbackSeconds)
    {
        if (!HasState(stateName))
        {
            if (fallbackSeconds > 0f) yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        animator.CrossFadeInFixedTime(stateName, crossFadeTime);
        Debug.Log($"[{name}] '{stateName}' 재생 요청 (Animator 위치: {animator.gameObject.name})", this);

        float timer = 0f;

        // 1) 그 상태로 들어갈 때까지 대기
        while (timer < maxActionTime && !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 2) 재생이 끝날 때까지 대기
        while (timer < maxActionTime)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName) || info.normalizedTime >= 1f) break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (timer >= maxActionTime)
        {
            Debug.LogWarning($"[{name}] '{stateName}' 상태가 {maxActionTime}초 안에 끝나지 않아 넘어갑니다. " +
                             "(상태에 진입하지 못했거나 반복 재생되는 클립일 수 있음)", this);
        }
    }

    private void ReturnToIdle()
    {
        if (HasState(idleState)) animator.CrossFadeInFixedTime(idleState, crossFadeTime);
    }

    private bool HasState(string stateName)
    {
        return animator != null
            && !string.IsNullOrEmpty(stateName)
            && animator.HasState(0, Animator.StringToHash(stateName));
    }

    // 상태 이름이 그대로 없으면 "이름 + 접미사"(예: Attack + Dragon)로 한 번 더 찾아봄
    private string ResolveState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return stateName;
        if (HasState(stateName)) return stateName;
        if (!string.IsNullOrEmpty(stateSuffix) && HasState(stateName + stateSuffix)) return stateName + stateSuffix;
        return stateName;
    }

    // 인스펙터의 상태 이름을 실제 Animator 상태 이름으로 맞추고, 없는 상태는 경고
    private void ResolveAllStates()
    {
        idleState = ResolveState(idleState);
        attackState = ResolveState(attackState);
        defendState = ResolveState(defendState);
        hurtState = ResolveState(hurtState);
        deathState = ResolveState(deathState);

        WarnIfMissing("Idle State", idleState);
        if (!useCodeAttack) WarnIfMissing("Attack State", attackState);
        WarnIfMissing("Defend State", defendState);
        WarnIfMissing("Hurt State", hurtState);
        WarnIfMissing("Death State", deathState);
    }

    private void WarnIfMissing(string label, string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || HasState(stateName)) return;

        Debug.LogWarning(
            $"[{name}] Animator에 '{stateName}' 상태가 없습니다. ({label}) " +
            "Animator 창에서 실제 상태 이름을 확인해 인스펙터에 입력하세요.", this);
    }

    // 인스펙터 ⋮ 메뉴에서 실행: 이 몬스터 Animator에 들어 있는 클립 이름 확인용
    [ContextMenu("Animator 클립 이름 출력")]
    private void LogClipNames()
    {
        Animator a = GetComponent<Animator>();
        if (a == null) a = GetComponentInChildren<Animator>();

        if (a == null || a.runtimeAnimatorController == null)
        {
            Debug.LogWarning("Animator 또는 Controller가 없습니다.", this);
            return;
        }

        foreach (AnimationClip clip in a.runtimeAnimatorController.animationClips)
        {
            Debug.Log($"클립: {clip.name} (길이 {clip.length:F2}초, 반복 {clip.isLooping})", this);
        }
    }
}