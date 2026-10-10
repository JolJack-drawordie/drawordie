using System.Collections;
using UnityEngine;

// 액트2 보스 전용 컨트롤러. DungeonMonsters2D 같은 "이미 애니메이션이 들어있는" 에셋을 그대로 써서
// 공격 / 방어(Cast) 2가지 의도를 갖는 보스를 만들 때 사용.
//
// [수정 사항]
// 1) 몸 위치 자동 보정: 에셋 프리팹(WoodGolem)이 루트에서 (-5.27, -1.1)만큼 떨어져 있어서
//    게임에서 보스가 스폰 포인트 옆으로 밀려 보이던 문제 -> 시작 시 "보이는 몸"의 중심을 루트 위치로 맞춤
// 2) 의도 아이콘: 몸의 실제 머리 꼭대기 위에 자동으로 띄움 (Canvas Pivot/크기 설정과 무관)
// 3) 공격/방어 모션: 루트 축이 아니라 "몸의 발밑"을 기준으로 기울기/크기 변화 -> 몸이 크게 휘둘리지 않음
//    월드 좌표로만 움직이고, 행동이 끝나면 위치/크기/각도/색/Animator를 무조건 원래대로 복구
// 4) Animator 상태 이름 자동 인식: 컨트롤러 상태 이름이 "AttackGolem", 클립 이름이 "AttackWoodGolem"처럼
//    서로 달라도 자동으로 찾아냄 (State Suffix 비워둬도 됨)
// 5) Animator Culling Mode를 Always Animate로 고정 (화면 밖 판정으로 멈추거나 안 보이는 문제 방지)
public class BossController2 : EnemyController
{
    public enum BossIntent { Attack, Defend }

    private Animator animator;
    private int baseAttackPower;
    private SpriteRenderer[] allRenderers;
    private Color[] baseColors;
    private Coroutine tintCoroutine;

    // 기본 자세 (월드 기준)
    private Vector3 basePos;
    private Quaternion baseRot;
    private Vector3 baseScale3;
    private Vector3 pivotLocal;      // 몸 발밑 중앙 (루트 로컬, 스케일 적용 전 값)
    private Vector3 pivotWorld;      // 몸 발밑 중앙 (월드)
    private Vector3 headOffset;      // 루트 -> 머리 꼭대기 중앙
    private Vector3 bodyCenterOffset;// 루트 -> 몸 중앙
    private Vector3 currentOffset;   // 돌진/흔들림으로 이동한 양
    private Quaternion canvasBaseRot;

    private BossIntent currentIntent = BossIntent.Attack;

    [Header("스폰 위치 보정 (스폰 포인트 기준으로 이동)")]
    public Vector3 spawnOffset = Vector3.zero;
    [Tooltip("시작할 때 보이는 몸의 가로 중심을 스폰 포인트(루트) 위치에 맞춤")]
    public bool autoCenterBody = true;

    [Header("의도 아이콘 위치 (몸의 머리 꼭대기 기준)")]
    public Vector3 iconOffsetAboveHead = new Vector3(0f, 0.6f, 0f);
    public int intentCanvasSortingBoost = 10;

    [Header("Animator 상태 이름 (자동 인식됨. 못 찾으면 State Suffix에 Golem 등을 입력)")]
    public string stateSuffix = "";
    public string idleState = "Idle";
    public string attackState = "Attack";
    public string moveState = "Move";             // 공격 때 다가가는 동안(돌진 구간) 재생
    public string hurtState = "Stuned";
    public string deathState = "Death";

    [Header("방어(Cast) 연출 - 전용 애니메이션이 없어도 코드로 펄스 + 색상 표시")]
    public string defendState = "Talk";   // 비워두면 코드 펄스만 재생. 지정하면 펄스와 함께 재생됨
    public Color guardTintColor = new Color(0.55f, 0.75f, 1f, 1f);
    public float guardTintDuration = 0.6f;
    public float pulseDuration = 0.8f;
    public int pulseCount = 1;
    public float pulseScale = 1.12f;
    public float pulseSquash = 0.06f;
    public float pulseRise = 0f;
    public float pulseShake = 0.03f;
    public float pulseSway = 3f;

    [Header("공격 모션 (코드 돌진)")]
    public bool playAttackClip = true;   // 타격 순간에 Attack 클립 재생
    public bool playMoveClip = true;     // 다가가는 동안(준비+돌진 구간) Move 클립 재생
    public float attackDistance = 4f;
    public float attackWindupBack = 0.25f;
    public float attackWindupTime = 0.2f;
    public float attackDashTime = 0.2f;
    public float attackStrikeTime = 0.15f;
    public float attackReturnTime = 0.3f;
    public float attackArcHeight = 0f;
    public float attackStrikeShake = 0.1f;
    public float attackLean = 6f;
    public float attackStrikeScale = 1.08f;

    [Header("의도 효과")]
    public int defendShield = 15;  // 방어(Cast) 턴에 보스가 얻는 쉴드

    [Header("재생 설정")]
    public float crossFadeTime = 0.05f;
    public float maxActionTime = 4f;
    public float fallbackDuration = 0.6f;

    [Header("단독 테스트용 (실제 전투에서는 꺼 두기)")]
    public bool useTestKey = false;
    public KeyCode testKey = KeyCode.Alpha3;

    protected override void Start()
    {
        transform.position += spawnOffset;

        base.Start(); // hitUnit 연결 + 피격 반짝임 구독

        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        Animator[] allAnimators = GetComponentsInChildren<Animator>();
        if (animator != null && allAnimators.Length > 1)
        {
            Debug.LogWarning(
                $"[{name}] Animator가 {allAnimators.Length}개 있습니다. 뼈대(몸 조각)가 있는 자식의 Animator만 " +
                $"동작하니 나머지는 제거하세요. (현재 사용 중: {animator.gameObject.name})", this);
        }

        if (animator == null)
        {
            Debug.LogError($"[{name}] Animator를 찾지 못했습니다!", this);
        }
        else
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ResolveAllStates();

            // Idle 자세를 한 번 즉시 적용해 둬야 (숨겨진 돌멩이 등이 빠진) 정확한 몸 크기를 잴 수 있음
            if (animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                if (HasState(idleState)) animator.Play(idleState, 0, 0f);
                animator.Update(0f);
            }
        }

        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++) baseColors[i] = allRenderers[i].color;

        // ----- 몸 위치 측정 + 자동 보정 -----
        Bounds body = GetBodyBounds();

        if (autoCenterBody)
        {
            float dx = transform.position.x - body.center.x;
            transform.position += new Vector3(dx, 0f, 0f);
            body.center += new Vector3(dx, 0f, 0f);
        }

        basePos = transform.position;
        baseRot = transform.rotation;
        baseScale3 = transform.localScale;
        currentOffset = Vector3.zero;

        pivotWorld = new Vector3(body.center.x, body.min.y, basePos.z);
        Vector3 local = Quaternion.Inverse(baseRot) * (pivotWorld - basePos);
        pivotLocal = new Vector3(
            SafeDiv(local.x, baseScale3.x),
            SafeDiv(local.y, baseScale3.y),
            SafeDiv(local.z, baseScale3.z));

        headOffset = new Vector3(body.center.x, body.max.y, basePos.z) - basePos;
        bodyCenterOffset = new Vector3(body.center.x, body.center.y, basePos.z) - basePos;

        // ----- 의도 아이콘 정렬 순서 -----
        if (intentCanvas != null && allRenderers.Length > 0)
        {
            SpriteRenderer top = allRenderers[0];
            for (int i = 1; i < allRenderers.Length; i++)
            {
                if (allRenderers[i].sortingOrder > top.sortingOrder) top = allRenderers[i];
            }

            intentCanvas.sortingLayerID = top.sortingLayerID;
            intentCanvas.sortingOrder = top.sortingOrder + intentCanvasSortingBoost;
            canvasBaseRot = intentCanvas.transform.rotation;
        }

        if (hitUnit != null) baseAttackPower = hitUnit.attackPower;

        if (hitUnit == null) Debug.LogWarning($"[{name}] EnemyUnit이 없어 체력/공격이 동작하지 않습니다.", this);
        else if (baseAttackPower <= 0) Debug.LogWarning($"[{name}] EnemyUnit의 Attack Power가 {baseAttackPower}입니다.", this);

        if (intentCanvas == null) Debug.LogWarning($"[{name}] Intent Canvas가 비어 있습니다.", this);
        if (intentIcon == null) Debug.LogWarning($"[{name}] Intent Icon이 비어 있습니다.", this);
        if (attackSprite == null || defendSprite == null)
            Debug.LogWarning($"[{name}] Attack/Defend Sprite 중 비어있는 게 있습니다.", this);

        DecideIntent();
    }

    private static float SafeDiv(float a, float b)
    {
        return Mathf.Abs(b) < 0.0001f ? 0f : a / b;
    }

    // 실제로 보이는 몸 조각들의 범위 (투명하게 숨겨진 돌멩이 같은 건 제외)
    private Bounds GetBodyBounds()
    {
        bool has = false;
        Bounds b = new Bounds(transform.position, Vector3.zero);

        for (int pass = 0; pass < 2 && !has; pass++)
        {
            foreach (SpriteRenderer r in allRenderers)
            {
                if (r == null || r.sprite == null || !r.gameObject.activeInHierarchy) continue;
                if (pass == 0 && (!r.enabled || r.color.a < 0.05f)) continue;

                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        return b;
    }

    // ===================== 자세 (위치/각도/크기) =====================

    // 몸 발밑을 기준으로 기울이고/늘리고, offset만큼 이동. 모든 모션은 이 함수 하나로만 transform을 바꿈
    private void ApplyPose(Vector3 offset, float tilt, float scaleX, float scaleY)
    {
        currentOffset = offset;

        Quaternion rot = baseRot * Quaternion.Euler(0f, 0f, tilt);
        Vector3 scale = new Vector3(baseScale3.x * scaleX, baseScale3.y * scaleY, baseScale3.z);

        transform.rotation = rot;
        transform.localScale = scale;
        transform.position = pivotWorld + offset - rot * Vector3.Scale(scale, pivotLocal);
    }

    private void ResetPose()
    {
        ApplyPose(Vector3.zero, 0f, 1f, 1f);
    }

    // ===================== 의도 아이콘 =====================

    private void PlaceIntentCanvas()
    {
        Vector3 target = basePos + currentOffset + headOffset + iconOffsetAboveHead;

        intentCanvas.transform.rotation = canvasBaseRot;

        if (intentIcon != null)
        {
            // 아이콘 이미지의 "중심"이 target에 오도록 Canvas를 이동 (Canvas 크기/Pivot과 무관)
            RectTransform iconRect = intentIcon.rectTransform;
            Vector3 iconCenter = iconRect.TransformPoint(iconRect.rect.center);
            Vector3 delta = target - iconCenter;
            delta.z = 0f;
            intentCanvas.transform.position += delta;
        }
        else
        {
            intentCanvas.transform.position = target;
        }
    }

    protected override void Update()
    {
        if (intentCanvas != null) PlaceIntentCanvas();

        if (useTestKey && Input.GetKeyDown(testKey) && !isActing && !hitIsDead)
        {
            ExecuteAction();
        }
    }

    // 테스트 키 / 외부 호출도 실제 전투와 같은 루틴을 타도록
    public override void ExecuteAction()
    {
        if (isActing || hitIsDead) return;
        StartCoroutine(PlayAttackAnimation());
    }

    // 다음 의도 2가지 중 랜덤 결정 (50/50)
    private void DecideIntent()
    {
        currentIntent = Random.value < 0.5f ? BossIntent.Attack : BossIntent.Defend;
        isNextAttack = currentIntent == BossIntent.Attack;
        UpdateIntentIcon();
    }

    private void UpdateIntentIcon()
    {
        if (intentIcon == null) return;

        switch (currentIntent)
        {
            case BossIntent.Attack: intentIcon.sprite = attackSprite; break;
            case BossIntent.Defend: intentIcon.sprite = defendSprite; break;
        }
    }

    // ===================== 턴 행동 =====================

    // TurnManager가 적 턴에 호출 (끝나면 TurnManager가 enemy.Attack을 호출함)
    public override IEnumerator PlayAttackAnimation()
    {
        if (isActing || hitIsDead) yield break;
        isActing = true;

        BossIntent acted = currentIntent;

        try
        {
            ApplyIntentEffect(acted);

            Debug.Log($"[{name}] 적 턴 의도: {acted} / 이번 턴 공격력: " +
                      $"{(hitUnit != null ? hitUnit.attackPower.ToString() : "EnemyUnit 없음")}", this);

            switch (acted)
            {
                case BossIntent.Attack:
                    yield return StartCoroutine(PlayCustomAttack());
                    break;
                case BossIntent.Defend:
                    yield return StartCoroutine(PlayCustomDefend());
                    break;
            }

            ApplyIntentEffect(acted); // 모션 중 값이 바뀌었어도 데미지 적용 직전에 다시 확정

            if (!hitIsDead) DecideIntent();
        }
        finally
        {
            RestoreAfterAction();
            isActing = false;
        }
    }

    // 행동이 끝나면 무조건 원래 상태로 (위치/크기/각도/색/Animator)
    private void RestoreAfterAction()
    {
        if (hitIsDead) return;

        ResetPose();

        if (tintCoroutine != null)
        {
            StopCoroutine(tintCoroutine);
            tintCoroutine = null;
        }

        if (allRenderers != null)
        {
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] == null) continue;
                allRenderers[i].enabled = true;
                allRenderers[i].color = baseColors[i];
            }
        }

        if (animator != null)
        {
            animator.speed = 1f;
            if (HasState(idleState) && !animator.GetCurrentAnimatorStateInfo(0).IsName(idleState))
                animator.CrossFadeInFixedTime(idleState, crossFadeTime);
        }
    }

    protected override IEnumerator PlayCustomAttack()
    {
        yield return StartCoroutine(DashMotionRoutine(
            attackDistance, attackWindupBack, attackWindupTime, attackDashTime,
            attackStrikeTime, attackReturnTime, attackArcHeight, attackStrikeShake,
            attackLean, attackStrikeScale,
            playMoveClip ? moveState : "", playAttackClip ? attackState : ""));

        if (!hitIsDead) ReturnToIdle();
    }

    protected override IEnumerator PlayCustomDefend()
    {
        // 다른 몬스터들이랑 동일하게: 색 변화(틴트) + 몸 커지는 펄스 (+ 있으면 Talk 애니메이션)
        if (guardTintDuration > 0f) StartTint(guardTintColor, guardTintDuration);
        if (HasState(defendState)) animator.CrossFadeInFixedTime(defendState, crossFadeTime);

        if (pulseDuration > 0f)
            yield return StartCoroutine(PulseRoutine());
        else
            yield return new WaitForSeconds(fallbackDuration);

        if (!hitIsDead) ReturnToIdle();
    }

    // 방어 펄스
    private IEnumerator PulseRoutine()
    {
        int count = Mathf.Max(1, pulseCount);
        float t = 0f;

        while (t < pulseDuration && !hitIsDead)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / pulseDuration);

            float w = Mathf.Sin(p * Mathf.PI * count);
            float k = w * w;

            float grow = (pulseScale - 1f) * k;
            float squash = pulseSquash * k;
            float shakeX = pulseShake > 0f ? Mathf.Sin(t * 60f) * pulseShake * k : 0f;
            float tilt = Mathf.Sin(p * Mathf.PI * 2f * count) * pulseSway * k;

            ApplyPose(new Vector3(shakeX, pulseRise * k, 0f), tilt, 1f + grow + squash, 1f + grow - squash);

            yield return null;
        }

        ResetPose();
    }

    // 공격 돌진 모션 (왼쪽 = 플레이어 방향으로 돌진했다가 복귀)
    private IEnumerator DashMotionRoutine(
        float distance, float windupBack, float windupTime, float dashTime,
        float strikeTime, float returnTime, float arcHeight, float strikeShake,
        float lean, float strikeScale,
        string moveClip, string strikeClip)
    {
        Vector3 target = new Vector3(-distance, 0f, 0f);

        try
        {
            Vector3 from = Vector3.zero;
            float tiltFrom = 0f;

            if (HasState(moveClip)) animator.CrossFadeInFixedTime(moveClip, crossFadeTime);

            if (windupBack > 0f && windupTime > 0f)
            {
                Vector3 back = new Vector3(windupBack, 0f, 0f);
                float backTilt = -lean * 0.5f;
                yield return StartCoroutine(MoveOverTime(Vector3.zero, back, windupTime, 0f, 0f, backTilt));
                from = back;
                tiltFrom = backTilt;
            }

            yield return StartCoroutine(MoveOverTime(from, target, dashTime, arcHeight, tiltFrom, lean));

            if (HasState(strikeClip)) animator.CrossFadeInFixedTime(strikeClip, crossFadeTime);

            float t = 0f;
            while (t < strikeTime && !hitIsDead)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / strikeTime);

                Vector3 shake = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * strikeShake;
                float s = Mathf.Lerp(1f, strikeScale, Mathf.Sin(p * Mathf.PI));
                ApplyPose(target + shake, lean, s, s);

                yield return null;
            }

            yield return StartCoroutine(MoveOverTime(target, Vector3.zero, returnTime, 0f, lean, 0f));
        }
        finally
        {
            ResetPose();
        }
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration, float arcHeight, float tiltFrom, float tiltTo)
    {
        float t = 0f;

        while (t < duration && !hitIsDead)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float e = p * p * (3f - 2f * p);

            Vector3 pos = Vector3.Lerp(from, to, e);
            pos.y += Mathf.Sin(p * Mathf.PI) * arcHeight;

            ApplyPose(pos, Mathf.Lerp(tiltFrom, tiltTo, e), 1f, 1f);

            yield return null;
        }

        ApplyPose(to, tiltTo, 1f, 1f);
    }

    // ===================== 피격 / 사망 =====================

    // 몸 조각이 여러 개인 캐릭터라서 모든 조각에 반짝임 적용
    protected override void PlayHitEffect()
    {
        StartTint(hitFlashColor, hitFlashDuration);

        if (hitEffectPrefab != null)
        {
            Vector3 center = basePos + currentOffset + bodyCenterOffset;
            Instantiate(hitEffectPrefab, center + hitEffectOffset, Quaternion.identity);
        }
    }

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

        tintCoroutine = null;
    }

    protected override void HandleHpChanged(int current, int max)
    {
        if (hitIsDead) return;

        bool tookDamage = current < hitLastHp;
        bool willDie = current <= 0 && hitUnit != null && hitUnit.statData != null && hitUnit.statData.maxHp > 0;

        base.HandleHpChanged(current, max);

        if (willDie)
        {
            PlayDeath();
        }
        else if (tookDamage && !isActing && HasState(hurtState))
        {
            StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        yield return StartCoroutine(PlayAndWait(hurtState, 0f));
        if (!isActing && !hitIsDead) ReturnToIdle();
    }

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

    // 실제 체력 시스템(EnemyUnit)에 의도 효과 적용
    private void ApplyIntentEffect(BossIntent intent)
    {
        if (hitUnit == null) return;

        switch (intent)
        {
            case BossIntent.Attack:
                hitUnit.attackPower = baseAttackPower;
                break;
            case BossIntent.Defend:
                hitUnit.attackPower = 0;
                hitUnit.AddShield(defendShield);
                break;
        }
    }

    // ===================== Animator 헬퍼 =====================

    private IEnumerator PlayAndWait(string stateName, float fallbackSeconds)
    {
        if (!HasState(stateName))
        {
            if (fallbackSeconds > 0f) yield return new WaitForSeconds(fallbackSeconds);
            yield break;
        }

        animator.CrossFadeInFixedTime(stateName, crossFadeTime);

        float timer = 0f;

        while (timer < maxActionTime && !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            timer += Time.deltaTime;
            yield return null;
        }

        while (timer < maxActionTime)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName) || info.normalizedTime >= 1f) break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void ReturnToIdle()
    {
        if (HasState(idleState)) animator.CrossFadeInFixedTime(idleState, crossFadeTime);
    }

    private bool HasState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return false;

        return animator.HasState(0, Animator.StringToHash(stateName))
            || animator.HasState(0, Animator.StringToHash("Base Layer." + stateName));
    }

    private string ResolveState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return stateName;
        if (HasState(stateName)) return stateName;
        if (!string.IsNullOrEmpty(stateSuffix) && HasState(stateName + stateSuffix)) return stateName + stateSuffix;

        if (animator == null || animator.runtimeAnimatorController == null) return stateName;

        // 컨트롤러 안의 클립 이름에서 상태 이름을 추측
        // 예) 클립 "AttackWoodGolem" -> "AttackWoodGolem", "AttackoodGolem", ... "AttackGolem" 순서로 상태가 있는지 확인
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip == null) continue;

            int idx = clip.name.IndexOf(stateName, System.StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            if (HasState(clip.name)) return clip.name;

            string tail = clip.name.Substring(idx + stateName.Length);
            for (int i = 0; i < tail.Length; i++)
            {
                string candidate = stateName + tail.Substring(i);
                if (HasState(candidate)) return candidate;
            }
        }

        return stateName;
    }

    private void ResolveAllStates()
    {
        idleState = ResolveState(idleState);
        attackState = ResolveState(attackState);
        moveState = ResolveState(moveState);
        defendState = ResolveState(defendState);
        hurtState = ResolveState(hurtState);
        deathState = ResolveState(deathState);

        Debug.Log($"[{name}] Animator 상태 인식 결과 - Idle:{idleState}, Attack:{attackState}, Move:{moveState}, " +
                  $"Defend:{defendState}, Hurt:{hurtState}, Death:{deathState}", this);

        WarnIfMissing("Idle State", idleState);
        WarnIfMissing("Attack State", attackState);
        WarnIfMissing("Move State", moveState);
        WarnIfMissing("Hurt State", hurtState);
        WarnIfMissing("Death State", deathState);
    }

    private void WarnIfMissing(string label, string stateName)
    {
        if (string.IsNullOrEmpty(stateName) || HasState(stateName)) return;

        Debug.LogWarning(
            $"[{name}] Animator에 '{stateName}' 상태가 없습니다. ({label}) " +
            "없어도 동작은 하지만(코드 모션만 재생) 연출이 밋밋할 수 있습니다.", this);
    }

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