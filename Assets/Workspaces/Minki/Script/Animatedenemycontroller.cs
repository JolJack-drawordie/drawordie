using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// 애니메이션 클립/Animator가 이미 들어 있는 몬스터(예: DungeonMonsters2D 에셋)용 범용 컨트롤러.
// 몬스터마다 스크립트를 새로 만들지 않고, 프리팹 인스펙터에서 Animator 상태 이름만 맞춰서 재사용한다.
public class AnimatedEnemyController : EnemyController
{
    public enum MotionPreset { Custom, Zombie, Vampire, Succubus }

    private Animator animator;
    private int baseAttackPower;   // 프리팹에 설정된 기본 공격력 (EnemyUnit)
    private SpriteRenderer[] allRenderers;   // 리깅 캐릭터는 몸 조각마다 SpriteRenderer가 따로 있음
    private Color[] baseColors;
    private Coroutine tintCoroutine;
    private Vector3 pulseBaseScale;
    private Vector3 pulseBasePos;
    private Quaternion pulseBaseRot;
    private Quaternion canvasBaseRot;   // 몬스터가 기울어져도 머리 위 아이콘은 똑바로 유지하기 위한 기준 회전

    [Header("스폰 위치 보정 (스폰 포인트 기준으로 이동)")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("의도 아이콘 위치 (몬스터 루트 위치 기준으로 머리 위에 띄울 거리)")]
    public Vector3 intentCanvasOffset = new Vector3(0f, 2f, 0f);
    public int intentCanvasSortingBoost = 10;   // 아이콘을 몸 조각들의 가장 높은 정렬 순서보다 얼마나 더 위에 그릴지

    [Header("Animator 상태 이름 (Animator 창의 상태 이름과 같아야 함)")]
    public string stateSuffix = "";       // 상태 이름이 IdleDragon, AttackDragon 처럼 끝에 이름이 붙어 있으면 "Dragon" 입력
    public string idleState = "Idle";
    public string attackState = "Attack";
    public string defendState = "";       // 비우면 애니메이션 없이 잠깐 대기
    public string hurtState = "";         // 비우면 피격 애니메이션 없이 반짝임만 재생
    public string deathState = "Death";   // 비우면 사망 애니메이션 없음

    [Header("방어 연출 (방어 애니메이션이 없어도 색으로 방어를 표시)")]
    public Color guardTintColor = new Color(0.55f, 0.75f, 1f, 1f);
    public float guardTintDuration = 0.5f;  // 0이면 사용 안 함

    [Header("몬스터별 모션 프리셋 (공격 모션 + 방어 펄스의 추천 값을 한 번에 적용)")]
    [FormerlySerializedAs("pulsePreset")]
    public MotionPreset motionPreset = MotionPreset.Custom;  // Custom이면 아래 숫자를 그대로 사용

    [Header("공격 모션 (코드) - Use Code Attack이 켜져 있으면 위치 이동(돌진)을 코드로 재생")]
    public bool useCodeAttack = true;
    public bool playAttackClip = true;      // 코드 돌진과 동시에 몬스터 고유 Attack 애니메이션 클립도 재생 (몬스터마다 동작이 달라 보이게 함)
    public float attackDistance = 4f;       // 플레이어 쪽(왼쪽)으로 돌진할 거리
    public float attackWindupBack = 0.2f;   // 돌진 전에 뒤(오른쪽)로 물러나는 거리 (0이면 생략)
    public float attackWindupTime = 0.2f;
    public float attackDashTime = 0.2f;
    public float attackStrikeTime = 0.15f;  // 타격 순간 머무는 시간
    public float attackReturnTime = 0.3f;
    public float attackArcHeight = 0f;      // 돌진 중 위로 뜨는 높이 (곡선 이동)
    public float attackStrikeShake = 0.1f;  // 타격 순간 떨림 세기
    public float attackLean = 0f;           // 돌진 중 앞으로 기울어지는 각도(도)
    public float attackStrikeScale = 1f;    // 타격 순간 크기 배율 (1이면 변화 없음)

    [Header("방어 펄스 (Defend State가 비어 있을 때 코드로 재생되는 방어 동작)")]
    public float pulseDuration = 0.6f;   // 전체 시간 (0이면 펄스 없이 대기만)
    public int pulseCount = 1;           // 부풀었다 줄어드는 횟수
    public float pulseScale = 1.1f;      // 가장 커졌을 때의 전체 크기 배율
    public float pulseSquash = 0f;       // + 가로로 넓고 낮게 / - 세로로 길고 좁게
    public float pulseRise = 0f;         // 위로 뜨는 높이
    public float pulseShake = 0f;        // 좌우 떨림 세기
    public float pulseSway = 0f;         // 좌우로 기울어지는 각도(도)

    [Header("재생 설정")]
    public float crossFadeTime = 0.05f;
    public float maxActionTime = 4f;       // 애니메이션이 안 끝나는 경우를 대비한 안전장치
    public float fallbackDuration = 0.6f;  // 재생할 상태가 없을 때 대기하는 시간

    [Header("의도 효과 (방어도는 팀에서 구현 예정이라 여기서 다루지 않음)")]
    public bool defendSkipsDamage = true;     // 방어 의도인 턴에는 공격력을 0으로 만들어 데미지를 주지 않음 (공격 턴의 데미지는 EnemyUnit의 Attack Power)

    [Header("단독 테스트용 (실제 전투에서는 꺼 두기)")]
    public bool useTestKey = false;
    public KeyCode testKey = KeyCode.Alpha3;

    protected override void Start()
    {
        // 스폰 포인트 위치에서 몬스터별로 위치 보정 (base.Start 전에 적용해야 originalPosition이 맞음)
        transform.position += spawnOffset;

        base.Start(); // 부모가 hitUnit(EnemyUnit) 연결 + 피격 반짝임 구독까지 처리

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

        // 펄스가 끝난 뒤 되돌아올 기준 상태 (spawnOffset 적용 이후 값)
        pulseBaseScale = transform.localScale;
        pulseBasePos = transform.localPosition;
        pulseBaseRot = transform.localRotation;
        ApplyMotionPreset();

        if (intentCanvas != null) canvasBaseRot = intentCanvas.transform.rotation;

        if (hitUnit != null) baseAttackPower = hitUnit.attackPower;

        // 프리팹 설정 누락 진단 (전투에서는 씬 오브젝트가 아니라 Resources/Monsters의 프리팹이 쓰임)
        if (hitUnit == null) Debug.LogWarning($"[{name}] EnemyUnit이 없어 체력/공격이 동작하지 않습니다.", this);
        else if (baseAttackPower <= 0) Debug.LogWarning($"[{name}] EnemyUnit의 Attack Power가 {baseAttackPower}입니다. 공격해도 데미지가 들어가지 않아요.", this);

        if (intentCanvas == null) Debug.LogWarning($"[{name}] Intent Canvas가 비어 있어 머리 위 아이콘 위치를 잡지 못합니다.", this);
        if (intentIcon == null) Debug.LogWarning($"[{name}] Intent Icon이 비어 있습니다.", this);
        if (attackSprite == null || defendSprite == null) Debug.LogWarning($"[{name}] Attack Sprite/Defend Sprite가 비어 있어 아이콘 이미지가 바뀌지 않습니다.", this);
    }

    // 아이콘 이미지의 "중심"이 (몬스터 루트 위치 + intentCanvasOffset)에 오도록 Canvas를 이동
    // -> Canvas의 크기/Pivot 설정과 상관없이 아이콘이 머리 위에 정확히 놓임
    private void PlaceIntentCanvas()
    {
        Vector3 target = transform.position + intentCanvasOffset;

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

    // 애니메이션 중심 몬스터라 부모의 꿀렁임(스케일 변화)은 쓰지 않음
    protected override void Update()
    {
        if (intentCanvas != null)
        {
            intentCanvas.transform.rotation = canvasBaseRot;
            PlaceIntentCanvas();
        }

        if (useTestKey && Input.GetKeyDown(testKey) && !isActing && !hitIsDead)
        {
            ExecuteAction();
        }
    }

    // TurnManager가 적 턴에 호출 (끝나면 TurnManager가 enemy.Attack을 호출함)
    public override IEnumerator PlayAttackAnimation()
    {
        if (isActing || hitIsDead) yield break;
        isActing = true;

        bool wasAttack = isNextAttack;

        try
        {
            // 1) 이번 턴 공격력 확정: 공격 = 기본 공격력 / 방어 = 0 (TurnManager가 모션 직후 enemy.Attack을 호출함)
            if (defendSkipsDamage) ApplyIntentEffect(wasAttack);

            Debug.Log($"[{name}] 적 턴 의도: {(wasAttack ? "공격" : "방어")} / 이번 턴 공격력: " +
                      $"{(hitUnit != null ? hitUnit.attackPower.ToString() : "EnemyUnit 없음")} " +
                      $"(방어 시 데미지 제외: {defendSkipsDamage})", this);

            yield return StartCoroutine(wasAttack ? PlayCustomAttack() : PlayCustomDefend());

            // 2) 모션이 끝난 직후, TurnManager가 데미지를 주기 직전에 한 번 더 확정 (모션 중 다른 곳에서 값이 바뀌어도 안전)
            if (defendSkipsDamage) ApplyIntentEffect(wasAttack);

            // 다음 의도를 새로 뽑고 아이콘 갱신 (사망 시에는 생략)
            if (!hitIsDead)
            {
                isNextAttack = Random.value > 0.5f;
                UpdateIntentUI();
            }
        }
        finally
        {
            // 중간에 중단되어도 isActing이 켜진 채로 남아 다음 턴이 통째로 건너뛰어지지 않도록 보장
            isActing = false;
        }
    }

    protected override IEnumerator PlayCustomAttack()
    {
        if (useCodeAttack)
        {
            // 돌진/타격은 코드로 재생하되, 몬스터 고유의 Attack 클립도 같이 틀어서
            // 몬스터마다 다른 동작(할퀴기/물기 등)이 보이도록 함
            if (playAttackClip && HasState(attackState))
                animator.CrossFadeInFixedTime(attackState, crossFadeTime);

            yield return StartCoroutine(AttackMotionRoutine());

            // Attack 클립이 Idle로 자동 복귀하지 않는 경우를 대비해 명시적으로 되돌림
            if (playAttackClip && !hitIsDead) ReturnToIdle();
        }
        else
        {
            yield return StartCoroutine(PlayAndWait(attackState, fallbackDuration));
            if (!hitIsDead) ReturnToIdle();
        }
    }

    protected override IEnumerator PlayCustomDefend()
    {
        // 방어 애니메이션 재생과 동시에 몸 색을 잠깐 바꿔서 "방어 중"임을 표시
        if (guardTintDuration > 0f) StartTint(guardTintColor, guardTintDuration);

        if (HasState(defendState))
        {
            // 방어 클립이 지정돼 있으면 그걸 재생
            yield return StartCoroutine(PlayAndWait(defendState, fallbackDuration));
            if (!hitIsDead) ReturnToIdle();
        }
        else if (pulseDuration > 0f)
        {
            // 없으면 코드로 만든 펄스 재생
            yield return StartCoroutine(PulseRoutine());
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
            float k = w * w; // 0 -> 1 -> 0 을 count번 부드럽게 반복

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

    // 공격 모션: 뒤로 물러났다가(준비) -> 플레이어 쪽으로 돌진 -> 타격(떨림) -> 제자리로 복귀
    // 몬스터별로 완전히 다른 동작이 필요하면 상속받아 이 함수를 override하면 됨
    protected virtual IEnumerator AttackMotionRoutine()
    {
        Vector3 start = pulseBasePos;
        Vector3 target = start + new Vector3(-attackDistance, 0f, 0f);

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

        // 3) 타격 (제자리에서 떨림 + 크기 펀치)
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

        // 4) 복귀
        yield return StartCoroutine(MoveOverTime(target, start, attackReturnTime, 0f, attackLean, 0f));

        // 원래 상태로 복원
        transform.localScale = pulseBaseScale;
        transform.localPosition = start;
        transform.localRotation = pulseBaseRot;
    }

    // from -> to 로 duration 동안 부드럽게 이동 (arcHeight: 중간에 위로 솟는 높이, tilt: z축 기울기)
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

    // 몬스터 특징에 맞춘 추천 모션 값 (공격 모션 + 방어 펄스)
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
                // 유연하고 우아함: 준비 없이 부드러운 곡선을 그리며 다가가 가볍게 타격하고 천천히 복귀
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

    // 몸 조각(SpriteRenderer)이 여러 개인 캐릭터라서, 부모의 반짝임(렌더러 1개만)을 모든 조각에 적용하도록 교체
    protected override void PlayHitEffect()
    {
        StartTint(hitFlashColor, hitFlashDuration);

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position + hitEffectOffset, Quaternion.identity);
        }
    }

    // 모든 몸 조각에 색을 곱했다가(원래 색 x tint) 일정 시간 뒤 원래 색으로 복원
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

    // 부모의 공통 반짝임(HandleHpChanged)에 피격/사망 애니메이션을 얹음
    protected override void HandleHpChanged(int current, int max)
    {
        if (hitIsDead) return;

        bool tookDamage = current < hitLastHp; // base가 hitLastHp를 갱신하기 전에 먼저 판단
        bool willDie = current <= 0;

        base.HandleHpChanged(current, max); // 공통 반짝임 재생 + hitIsDead 플래그 + hitLastHp 갱신

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

        // 그 사이에 다른 행동이 시작됐거나 죽었다면 Idle로 되돌리지 않음
        if (!isActing && !hitIsDead) ReturnToIdle();
    }

    private void PlayDeath()
    {
        isActing = true; // 이후 어떤 행동도 하지 않도록 막음

        if (intentIcon != null) intentIcon.gameObject.SetActive(false);

        if (HasState(deathState))
        {
            animator.CrossFadeInFixedTime(deathState, crossFadeTime);
            StartCoroutine(FreezeOnDeathEnd());
        }
    }

    // Death 애니메이션이 거의 끝나는 지점에서 멈춰 세움 (Death -> Idle 전환이 있어도 다시 일어나지 않도록)
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

    // 실제 체력 시스템(EnemyUnit)에 의도 효과 적용 (공격: 기본 공격력 / 방어: 공격 없음)
    private void ApplyIntentEffect(bool isAttack)
    {
        if (hitUnit == null) return;

        if (isAttack)
        {
            hitUnit.attackPower = baseAttackPower;
        }
        else
        {
            hitUnit.attackPower = 0;          // 이번 턴은 공격하지 않음
        }
    }

    // 상태를 재생하고, 해당 상태가 끝날 때까지 대기 (상태가 없으면 fallback 시간만큼 대기)
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

        // 1) 해당 상태로 진입할 때까지 대기
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

    // 컴포넌트 우측 상단 ⋮ 메뉴에서 실행: 이 몬스터 Animator에 들어 있는 클립 이름 확인용
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