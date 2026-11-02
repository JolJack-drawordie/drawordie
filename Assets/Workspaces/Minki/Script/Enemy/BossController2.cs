using System.Collections;
using UnityEngine;

// 중간보스 / 보스 컨트롤러. Animator와 클립이 들어 있는 에셋(DungeonMonsters2D)을 그대로 쓴다.
// - 의도: 공격 / 방어 / 기 모으기 / 강공격 (패턴 순환 또는 무작위)
// - 기믹: 패턴, 기 모으기 → 강공격, 재생, 분노, 흡혈, 가시 갑옷, 2페이즈 (서버 Monster.json의 boss 블록이 우선, 없으면 인스펙터 값)
// - 자세 보정: 에셋마다 몸이 루트에서 떨어져 있어서, 시작할 때 보이는 몸의 중심 / 발밑을 기준으로 위치를 맞추고
//   모든 모션(돌진, 펄스, 떨림)을 몸 발밑 기준으로 적용한 뒤 끝나면 원래 자세로 되돌린다
// - 의도 아이콘은 몸의 실제 머리 꼭대기 위에 띄운다
public class BossController2 : EnemyController
{
    // Charge = 기 모으기(이번 턴 공격 안 함) → 다음 턴은 무조건 StrongAttack(강공격)
    public enum BossIntent { Attack, Defend, Charge, StrongAttack }

    private Animator animator;
    private int baseAttackPower; // EnemyUnit의 기본 공격력 (분노 / 강공격 배율 계산 기준)
    private SpriteRenderer[] allRenderers; // 몸 조각마다 SpriteRenderer가 따로 있음
    private Color[] baseColors; // 몸 조각별 원래 색
    private Coroutine tintCoroutine;

    // 기본 자세 (월드 기준). 모든 모션은 이 자세에서 offset / 기울기 / 크기만 바꾼다
    private Vector3 basePos;
    private Quaternion baseRot;
    private Vector3 baseScale3;
    private Vector3 pivotLocal; // 몸 발밑 가운데 (루트 로컬, 크기 적용 전 값)
    private Vector3 pivotWorld; // 몸 발밑 가운데 (월드)
    private Vector3 headOffset; // 루트 → 머리 꼭대기 가운데
    private Vector3 bodyCenterOffset; // 루트 → 몸 가운데
    private Vector3 currentOffset; // 돌진 / 흔들림으로 이동한 양
    private Quaternion canvasBaseRot;

    private BossIntent currentIntent = BossIntent.Attack;
    private bool currentActionStrong; // 이번 행동이 강공격인지 (모션 / 효과음 / 흔들림 세기용)

    // 기믹 진행 상태
    private int patternIndex;
    private bool isEnraged;
    private bool isPhase2;
    private Color persistentTint = Color.white; // 분노 / 2페이즈 동안 계속 유지되는 몸 색
    // 의도 아이콘의 원래 크기 / 색 (기 모으기 / 강공격 때 바꿨다가 되돌림)
    private Vector3 iconBaseScale = Vector3.one;
    private Color iconBaseColor = Color.white;

    [Header("스폰 위치 보정 (스폰 포인트 기준으로 이동)")]
    public Vector3 spawnOffset = Vector3.zero;
    [Tooltip("시작할 때 보이는 몸의 가로 중심을 스폰 포인트(루트) 위치에 맞춤")]
    public bool autoCenterBody = true;
    [Tooltip("시작할 때 보이는 몸의 발밑 높이를 플레이어 발밑 높이에 맞춤 (켜면 spawnOffset.y는 무시됨)")]
    public bool alignFeetToPlayer = false;

    [Header("의도 아이콘 위치 (몸의 머리 꼭대기 기준)")]
    public Vector3 iconOffsetAboveHead = new Vector3(0f, 0.6f, 0f);
    public int intentCanvasSortingBoost = 10;

    [Header("Animator 상태 이름 (자동 인식됨. 못 찾으면 State Suffix에 Golem 등을 입력)")]
    public string stateSuffix = "";
    public string idleState = "Idle";
    public string attackState = "Attack";
    public string moveState = "Move"; // 공격 때 다가가는 동안(돌진 구간) 재생
    public string hurtState = "Stuned";
    public string deathState = "Death";
    [Tooltip("피격 모션을 이 시간(초)만 재생하고 Idle로 돌아감. Stuned 클립은 2~3초짜리 기절 모션이라 앞부분만 짧게 씀. 0 이하면 클립 끝까지 재생")]
    public float hurtMaxDuration = 0.4f;

    [Header("방어(Cast) 연출 - 전용 애니메이션이 없어도 코드로 펄스 + 색상 표시")]
    public string defendState = "Talk"; // 방어 때 펄스와 함께 재생할 클립 (비우면 코드 펄스만)
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
    public bool playAttackClip = true; // 타격 순간에 Attack 클립 재생
    public bool playMoveClip = true; // 다가가는 동안(준비 + 돌진) Move 클립 재생
    public float attackDistance = 4f;
    [Tooltip("켜면 Attack Distance 대신 플레이어 바로 앞까지 거리를 자동 계산해서 돌진")]
    public bool dashToPlayer = false;
    public float attackStopGap = 0.3f; // 플레이어 몸 앞에서 멈출 간격 (클수록 덜 붙음)
    public float maxDashDistance = 15f; // 자동 계산 거리의 최대값 (안전장치)
    public float attackWindupBack = 0.25f;
    public float attackWindupTime = 0.2f;
    public float attackDashTime = 0.2f;
    public float attackStrikeTime = 0.15f;
    public float attackReturnTime = 0.3f;
    public float attackArcHeight = 0f;
    public float attackStrikeShake = 0.1f;
    public float attackLean = 6f;
    public float attackStrikeScale = 1.08f;

    [Header("공격 모션 타이밍 (공격 클립이 끝난 뒤에 돌아가기)")]
    public bool waitAttackClipBeforeReturn = true; // 공격 클립을 끝까지 보여 준 뒤 복귀
    [Range(0.3f, 1f)] public float attackClipEndRatio = 0.9f; // 클립이 이 비율까지 재생되면 복귀 시작
    public float attackReturnDelay = 0.15f; // 클립이 끝나고 잠깐 머무는 시간

    [Header("강공격 (차지 다음 턴) 동작")]
    public string specialAttackState = "SpecialAttack"; // 없으면 일반 Attack 클립 사용
    [Tooltip("강공격 효과음 (비워두면 일반 공격 효과음)")]
    public AudioClip strongAttackSound;
    public float strongAttackShakeMultiplier = 1.8f;

    [Header("몸 기준 조각 (위치 보정/아이콘 위치 계산용)")]
    public string bodyPartName = "Body"; // 가로 중심 기준 조각 (없으면 몸 전체 범위)
    public string headPartName = "Head"; // 아이콘 가로 위치 기준 조각 (없으면 몸 기준)
    public string[] boundsIgnoreKeywords = { "Weapon", "Effect", "Fire", "Smoke", "Spell", "Projectile", "Stone" }; // 몸 범위를 잴 때 빼는 조각 이름 (무기, 이펙트 등)

    [Header("의도 효과")]
    public int defendShield = 15; // 방어 턴에 얻는 방어도 (서버 Monster.json의 defend가 있으면 그 값)

    [Header("기믹 - 패턴 순환 (끄면 공격/방어 랜덤)")]
    public bool usePattern = false;
    public BossIntent[] pattern = { BossIntent.Attack, BossIntent.Attack, BossIntent.Defend };

    [Header("기믹 - 차지 공격 (기 모으기 → 다음 턴 강공격)")]
    public bool useCharge = false;
    [Range(0f, 1f)] public float chargeChance = 0.3f; // 패턴을 안 쓸 때 기 모으기가 나올 확률
    public float chargeMultiplier = 2.5f; // 강공격 배율
    public Sprite chargeSprite; // 비우면 공격 아이콘을 노란색으로 표시
    public Sprite strongAttackSprite; // 비우면 공격 아이콘을 빨갛고 크게 표시
    public Color chargeIconColor = new Color(1f, 0.85f, 0.3f, 1f);
    public Color strongAttackIconColor = new Color(1f, 0.45f, 0.45f, 1f);
    public Color chargeTintColor = new Color(1f, 0.9f, 0.45f, 1f);
    [Tooltip("기 모으기 때 재생할 Animator 상태 (비우면 코드 동작만: 웅크리며 떨림 + 노란 번쩍임). 방어(Talk)와 다른 걸 쓰세요")]
    public string chargeState = ""; // 비우면 코드 동작만 (웅크리며 떨림 + 노란 번쩍임). 방어(Talk)와 다른 상태를 쓸 것
    public float chargeDuration = 1.1f;
    public float chargeShake = 0.06f; // 떨림 세기 (점점 세짐)
    public float chargeCrouch = 0.08f; // 웅크리는 정도
    public int chargeFlashCount = 3; // 노랗게 번쩍이는 횟수

    [Header("기믹 - 재생 (매 턴 체력 회복)")]
    public bool useRegen = false;
    public int regenAmount = 4;

    [Header("기믹 - 분노 (체력이 일정 비율 이하가 되면 공격력 증가)")]
    public bool useEnrage = false;
    [Range(0f, 1f)] public float enrageHpRatio = 0.5f;
    public float enrageAttackMultiplier = 1.5f;
    public Color enrageTintColor = new Color(1f, 0.6f, 0.6f, 1f);

    [Header("기믹 - 흡혈 (플레이어에게 준 피해의 일부만큼 회복)")]
    public bool useLifesteal = false;
    [Range(0f, 1f)] public float lifestealRatio = 0.5f;

    [Header("기믹 - 가시 갑옷 (방어도가 있을 때 맞으면 반사 피해)")]
    public bool useThorns = false;
    public int thornsDamage = 3;

    [Header("기믹 - 페이즈 전환 (체력이 일정 비율 이하가 되면 1회)")]
    public bool usePhase2 = false;
    [Range(0f, 1f)] public float phase2HpRatio = 0.5f;
    public int phase2Shield = 20;
    public int phase2Heal = 0;
    public BossIntent[] phase2Pattern; // 비우면 기존 패턴 / 무작위 유지
    public Color phase2TintColor = new Color(1f, 0.5f, 0.35f, 1f);

    [Header("재생 설정")]
    public float crossFadeTime = 0.05f;
    public float maxActionTime = 4f;
    public float fallbackDuration = 0.6f;

    protected override void Start()
    {
        transform.position += spawnOffset;

        base.Start(); // EnemyUnit 연결 + 피격 이벤트 구독

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
            // 루트 모션은 쓰지 않고, 화면 밖 판정으로 애니메이션이 멈추지 않게 항상 재생
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ResolveAllStates();

            // Idle 자세를 한 번 바로 적용해 둬야 (숨겨진 돌멩이 등이 빠진) 정확한 몸 크기를 잴 수 있음
            if (animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                if (HasState(idleState)) animator.Play(idleState, 0, 0f);
                animator.Update(0f);
            }
        }

        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        PrepareBodyRenderers(allRenderers);
        baseColors = new Color[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++) baseColors[i] = allRenderers[i].color;

        // ----- 몸 위치 측정 + 자동 보정 -----
        // body: 무기 / 이펙트를 뺀 전체 범위 (발밑, 머리 꼭대기), core: Body 조각 (가로 중심), head: Head 조각 (아이콘 가로 위치)
        Bounds body = GetBodyBounds();
        Bounds core = GetPartBounds(bodyPartName, body);
        Bounds head = GetPartBounds(headPartName, core);

        // 보이는 몸의 가로 중심을 스폰 위치에 맞춤
        if (autoCenterBody)
        {
            Vector3 d = new Vector3(transform.position.x - core.center.x, 0f, 0f);
            transform.position += d;
            body.center += d; core.center += d; head.center += d;
        }

        // 보이는 몸의 발밑을 플레이어 발밑 높이에 맞춤
        if (alignFeetToPlayer && TryGetPlayerFeetY(out float playerFeetY))
        {
            Vector3 d = new Vector3(0f, playerFeetY - body.min.y, 0f);
            transform.position += d;
            body.center += d; core.center += d; head.center += d;
        }

        // 모션의 기준이 되는 자세와 몸 발밑 / 머리 / 몸 가운데 위치
        basePos = transform.position;
        baseRot = transform.rotation;
        baseScale3 = transform.localScale;
        currentOffset = Vector3.zero;

        pivotWorld = new Vector3(core.center.x, body.min.y, basePos.z);
        Vector3 local = Quaternion.Inverse(baseRot) * (pivotWorld - basePos);
        pivotLocal = new Vector3(
            SafeDiv(local.x, baseScale3.x),
            SafeDiv(local.y, baseScale3.y),
            SafeDiv(local.z, baseScale3.z));

        headOffset = new Vector3(head.center.x, Mathf.Max(body.max.y, head.max.y), basePos.z) - basePos;
        bodyCenterOffset = new Vector3(core.center.x, body.center.y, basePos.z) - basePos;

        // ----- 의도 아이콘이 몸 조각보다 위에 그려지도록 -----
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

        if (hitUnit != null)
        {
            baseAttackPower = hitUnit.attackPower;

            // 방어량과 기믹 수치는 서버 Monster.json 값을 우선 사용 (없을 때만 인스펙터 값)
            if (hitUnit.statData != null && hitUnit.statData.defend > 0)
                defendShield = hitUnit.statData.defend;

            if (hitUnit.statData != null) ApplyServerGimmicks(hitUnit.statData.boss);

            // 가시 갑옷 / 흡혈 기믹용 이벤트
            hitUnit.OnDamaged += HandleDamagedForThorns;
            hitUnit.OnAttackHit += HandleAttackHitForLifesteal;
        }

        if (intentIcon != null)
        {
            iconBaseScale = intentIcon.rectTransform.localScale;
            iconBaseColor = intentIcon.color;
        }

        if (hitUnit == null) Debug.LogWarning($"[{name}] EnemyUnit이 없어 체력/공격이 동작하지 않습니다.", this);
        else if (baseAttackPower <= 0) Debug.LogWarning($"[{name}] EnemyUnit의 Attack Power가 {baseAttackPower}입니다.", this);

        if (intentCanvas == null) Debug.LogWarning($"[{name}] Intent Canvas가 비어 있습니다.", this);
        if (intentIcon == null) Debug.LogWarning($"[{name}] Intent Icon이 비어 있습니다.", this);
        if (attackSprite == null || defendSprite == null)
            Debug.LogWarning($"[{name}] Attack/Defend Sprite 중 비어있는 게 있습니다.", this);

        // 불러온 전투면 저장된 의도 / 기믹 상태로 복원, 아니면 새로 결정
        if (!RestoreBossState())
            DecideIntent();
    }

    // ===== 저장 / 불러오기 =====

    // 의도 + 기믹 진행 상태 저장
    public override void WriteSaveState(MonsterSaveData data)
    {
        data.hasIntent = true;
        data.intent = currentIntent.ToString();
        data.intentValue = currentIntent == BossIntent.Defend ? defendShield : GetAttackPower(currentIntent);
        data.patternIndex = patternIndex;
        data.isEnraged = isEnraged;
        data.isPhase2 = isPhase2;
    }

    // 부모(EnemyController.Start)의 일반 몬스터 복원은 건너뜀. 보스는 Start 끝의 RestoreBossState에서 처리
    protected override bool RestoreSavedIntent()
    {
        return HasSavedIntent;
    }

    // 저장된 의도 / 기믹 상태 복원 (서버 기믹과 기본 공격력이 준비된 Start 끝에서 호출)
    private bool RestoreBossState()
    {
        if (!HasSavedIntent) return false;

        if (!System.Enum.TryParse(savedState.intent, true, out BossIntent intent))
            return false;

        // 기 모으기 기믹이 꺼진 보스면 DecideIntent와 같은 규칙으로 일반 공격 취급
        if (!useCharge && (intent == BossIntent.Charge || intent == BossIntent.StrongAttack))
            intent = BossIntent.Attack;

        currentIntent = intent;
        patternIndex = Mathf.Max(0, savedState.patternIndex);
        isEnraged = useEnrage && savedState.isEnraged;
        isPhase2 = usePhase2 && savedState.isPhase2;

        // 분노 / 2페이즈 몸 색 복원 (CheckHpGimmicks와 같은 우선순위: 2페이즈 색이 우선)
        if (isPhase2) persistentTint = phase2TintColor;
        else if (isEnraged) persistentTint = enrageTintColor;
        ApplyPersistentColors();

        isNextAttack = currentIntent == BossIntent.Attack || currentIntent == BossIntent.StrongAttack;

        // 아이콘 + 공격력(분노 배율 포함) 갱신
        UpdateIntentIcon();

        Debug.Log($"[{name}] 저장된 상태 복원 - 의도:{currentIntent} 패턴:{patternIndex} 분노:{isEnraged} 2페이즈:{isPhase2}", this);
        return true;
    }

    // 보스 몸 앞(왼쪽 끝)이 플레이어 몸 앞(오른쪽 끝) + attackStopGap 위치까지 가는 거리
    private bool TryGetDashDistanceToPlayer(out float distance)
    {
        distance = attackDistance;

        // 플레이어는 칼 / 이펙트를 뺀 몸통 앞 (전체 범위를 쓰면 칼끝 앞에 멈춰 공격이 닿지 않음)
        if (!PlayerBodyBounds.TryGetFrontX(out float playerFront)) return false;

        // 행동 시작 시점(제자리)의 보스 몸 범위. 무기 / 이펙트 조각은 GetBodyBounds에서 빠짐
        float bossFront = GetBodyBounds().min.x;

        float d = bossFront - playerFront - attackStopGap;
        if (d <= 0f) return false;

        distance = Mathf.Min(d, maxDashDistance);
        return true;
    }

    // 플레이어(Spine)의 보이는 몸 범위에서 발밑 높이
    private bool TryGetPlayerFeetY(out float feetY)
    {
        feetY = 0f;
        PlayerUnit player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null) return false;

        MeshRenderer body = player.GetComponentInChildren<MeshRenderer>();
        feetY = body != null ? body.bounds.min.y : player.transform.position.y;
        return true;
    }

    private static float SafeDiv(float a, float b)
    {
        return Mathf.Abs(b) < 0.0001f ? 0f : a / b;
    }

    // 이름에 무기 / 이펙트 키워드가 들어간 조각인지
    private bool IsIgnoredPart(SpriteRenderer r)
    {
        if (boundsIgnoreKeywords == null) return false;
        foreach (string k in boundsIgnoreKeywords)
        {
            if (!string.IsNullOrEmpty(k) && r.gameObject.name.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    // 이름이 partName인 조각의 범위 (정확히 같은 이름 우선, 없으면 이름에 포함된 것). 못 찾으면 fallback
    private Bounds GetPartBounds(string partName, Bounds fallback)
    {
        if (string.IsNullOrEmpty(partName) || allRenderers == null) return fallback;

        for (int exact = 1; exact >= 0; exact--)
        {
            foreach (SpriteRenderer r in allRenderers)
            {
                if (r == null || r.sprite == null || !r.gameObject.activeInHierarchy) continue;
                string n = r.gameObject.name;
                bool match = exact == 1
                    ? string.Equals(n, partName, System.StringComparison.OrdinalIgnoreCase)
                    : n.IndexOf(partName, System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (match) return r.bounds;
            }
        }

        return fallback;
    }

    // 실제로 보이는 몸 조각들의 범위. 못 찾으면 조건을 조금씩 풀어서 다시 찾는다
    // (1차: 숨겨진 조각 / 무기 / 이펙트 제외 → 2차: 숨겨진 조각만 제외 → 3차: 전부)
    private Bounds GetBodyBounds()
    {
        bool has = false;
        Bounds b = new Bounds(transform.position, Vector3.zero);

        for (int pass = 0; pass < 3 && !has; pass++)
        {
            foreach (SpriteRenderer r in allRenderers)
            {
                if (r == null || r.sprite == null || !r.gameObject.activeInHierarchy) continue;
                if (pass == 0 && (!r.enabled || r.color.a < 0.05f || IsIgnoredPart(r))) continue;
                if (pass == 1 && (!r.enabled || r.color.a < 0.05f)) continue;

                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        return b;
    }

    // ===== 자세 (위치 / 각도 / 크기) =====

    // 몸 발밑을 기준으로 기울이고 늘린 뒤 offset만큼 이동. 모든 모션은 이 함수로만 transform을 바꾼다
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

    // ===== 의도 아이콘 =====

    // 아이콘 이미지의 중심이 (몸 머리 꼭대기 + 오프셋)에 오도록 캔버스를 옮긴다 (돌진 중에는 몸을 따라감)
    private void PlaceIntentCanvas()
    {
        Vector3 target = basePos + currentOffset + headOffset + iconOffsetAboveHead;

        intentCanvas.transform.rotation = canvasBaseRot;

        if (intentIcon != null)
        {
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

    // 부모의 숨쉬기 연출은 쓰지 않고 아이콘 위치만 갱신
    protected override void Update()
    {
        if (intentCanvas != null) PlaceIntentCanvas();
    }

    // 적 턴에 TurnManager가 호출
    public override IEnumerator ExecuteAction()
    {
        if (isActing || hitIsDead) yield break;
        yield return StartCoroutine(PlayAttackAnimation());
    }

    // 서버 Monster.json의 "boss" 블록 적용
    // - 블록이 없으면 프리팹 인스펙터 값 유지
    // - 블록이 있으면 서버 기준: 블록에 없는 기믹은 꺼지고, 숫자가 -1(안 적음)인 항목만 인스펙터 값 유지
    private void ApplyServerGimmicks(BossGimmickData g)
    {
        if (g == null || !g.defined) return;

        BossIntent[] serverPattern = ParsePattern(g.pattern);
        usePattern = serverPattern.Length > 0;
        if (usePattern) pattern = serverPattern;

        useCharge = g.charge != null && g.charge.defined;
        if (useCharge)
        {
            if (g.charge.chance >= 0f) chargeChance = Mathf.Clamp01(g.charge.chance);
            if (g.charge.multiplier >= 0f) chargeMultiplier = g.charge.multiplier;
        }

        useRegen = g.regen != null && g.regen.defined;
        if (useRegen && g.regen.amount >= 0) regenAmount = g.regen.amount;

        useEnrage = g.enrage != null && g.enrage.defined;
        if (useEnrage)
        {
            if (g.enrage.hpRatio >= 0f) enrageHpRatio = Mathf.Clamp01(g.enrage.hpRatio);
            if (g.enrage.attackMultiplier >= 0f) enrageAttackMultiplier = g.enrage.attackMultiplier;
        }

        useLifesteal = g.lifesteal != null && g.lifesteal.defined;
        if (useLifesteal && g.lifesteal.ratio >= 0f) lifestealRatio = Mathf.Clamp01(g.lifesteal.ratio);

        useThorns = g.thorns != null && g.thorns.defined;
        if (useThorns && g.thorns.damage >= 0) thornsDamage = g.thorns.damage;

        usePhase2 = g.phase2 != null && g.phase2.defined;
        if (usePhase2)
        {
            if (g.phase2.hpRatio >= 0f) phase2HpRatio = Mathf.Clamp01(g.phase2.hpRatio);
            if (g.phase2.shield >= 0) phase2Shield = g.phase2.shield;
            if (g.phase2.heal >= 0) phase2Heal = g.phase2.heal;
            BossIntent[] p2 = ParsePattern(g.phase2.pattern);
            if (p2.Length > 0) phase2Pattern = p2;
        }

        Debug.Log($"[{name}] 서버 기믹 적용 - 패턴:{usePattern} 차지:{useCharge} 재생:{useRegen} 분노:{useEnrage} " +
                  $"흡혈:{useLifesteal} 가시:{useThorns} 2페이즈:{usePhase2}", this);
    }

    // ["Attack","Defend",...] → BossIntent[] (대소문자 무시, 모르는 이름은 경고 후 건너뜀)
    private BossIntent[] ParsePattern(System.Collections.Generic.List<string> names)
    {
        var result = new System.Collections.Generic.List<BossIntent>();
        if (names == null) return result.ToArray();

        foreach (string s in names)
        {
            if (System.Enum.TryParse(s, true, out BossIntent intent)) result.Add(intent);
            else Debug.LogWarning($"[{name}] 서버 패턴에 알 수 없는 행동 '{s}' (Attack/Defend/Charge/StrongAttack 중 하나)", this);
        }
        return result.ToArray();
    }

    // 다음 의도 결정: 기 모으기 다음엔 무조건 강공격 → 패턴 → 기 모으기 확률 → 공격 / 방어 무작위 순서
    private void DecideIntent()
    {
        BossIntent previous = currentIntent;

        if (useCharge && previous == BossIntent.Charge)
        {
            currentIntent = BossIntent.StrongAttack;
        }
        else
        {
            BossIntent[] activePattern = (isPhase2 && phase2Pattern != null && phase2Pattern.Length > 0) ? phase2Pattern : pattern; // 2페이즈 전용 패턴이 있으면 그것을 사용

            if (usePattern && activePattern != null && activePattern.Length > 0)
            {
                currentIntent = activePattern[patternIndex % activePattern.Length];
                patternIndex++;
            }
            else if (useCharge && Random.value < chargeChance)
            {
                currentIntent = BossIntent.Charge;
            }
            else
            {
                currentIntent = Random.value < 0.5f ? BossIntent.Attack : BossIntent.Defend;
            }

            // 기 모으기 기믹을 끈 상태에서 패턴에 Charge / StrongAttack이 들어 있으면 일반 공격으로 취급
            if (!useCharge && (currentIntent == BossIntent.Charge || currentIntent == BossIntent.StrongAttack))
                currentIntent = BossIntent.Attack;
        }

        isNextAttack = currentIntent == BossIntent.Attack || currentIntent == BossIntent.StrongAttack;

        UpdateIntentIcon();
    }

    // 의도에 맞게 아이콘 / 숫자 / 이번 턴 공격력 갱신
    private void UpdateIntentIcon()
    {
        if (intentIcon == null) return;

        Vector3 scale = iconBaseScale;
        Color color = iconBaseColor;

        ApplyIntentEffect(currentIntent);

        switch (currentIntent)
        {
            case BossIntent.Attack:
                intentIcon.sprite = GetIntentSprite(true, hitUnit.attackPower);
                actionText.text = hitUnit.attackPower.ToString();
                break;
            case BossIntent.Defend:
                intentIcon.sprite = GetIntentSprite(false, defendShield);
                actionText.text = defendShield.ToString();
                break;
            case BossIntent.Charge:
                // 방어 아이콘과 헷갈리지 않도록, 전용 아이콘이 없으면 공격 아이콘을 노란색으로
                intentIcon.sprite = IntentIconSet.ResolveCharge(MonsterId, chargeSprite != null ? chargeSprite : GetIntentSprite(true, baseAttackPower));
                color = chargeIconColor;
                actionText.text = "";
                break;
            case BossIntent.StrongAttack:
                // 강공격 전용 아이콘이 우선, 없으면 수치 구간별 공격 아이콘을 빨갛고 크게
                intentIcon.sprite = IntentIconSet.ResolveStrongAttack(MonsterId, strongAttackSprite != null ? strongAttackSprite : GetIntentSprite(true, hitUnit.attackPower));
                color = strongAttackIconColor;
                scale = iconBaseScale * 1.4f;
                actionText.text = hitUnit.attackPower.ToString();
                break;
        }

        intentIcon.rectTransform.localScale = scale;
        intentIcon.color = color;
    }

    // 의도 아이콘 설명: 이번 행동 + 켜져 있는 기믹
    public override string GetIntentTooltip()
    {
        var sb = new System.Text.StringBuilder();

        switch (currentIntent)
        {
            case BossIntent.Attack:
                sb.Append($"<color={TooltipAttackColor}><size=32>공격</size></color>\n피해 {GetAttackPower(BossIntent.Attack)}");
                break;
            case BossIntent.Defend:
                sb.Append($"<color={TooltipDefendColor}><size=32>방어</size></color>\n방어도 +{defendShield}");
                break;
            case BossIntent.Charge:
                sb.Append($"<color={TooltipChargeColor}><size=32>차지 중</size></color>\n기를 모으는 중 (이번 턴 공격 안 함)\n" +
                          $"다음 턴 강공격: 피해 {GetAttackPower(BossIntent.StrongAttack)}");
                break;
            case BossIntent.StrongAttack:
                sb.Append($"<color={TooltipAttackColor}><size=32>강공격</size></color>\n모은 기를 터뜨려 피해 {GetAttackPower(BossIntent.StrongAttack)}");
                break;
        }

        // 켜져 있는 기믹 설명 (작은 회색 글씨로 아래에)
        var notes = new System.Collections.Generic.List<string>();

        if (useRegen && regenAmount > 0)
            notes.Add($"재생: 매 턴 체력 +{regenAmount}");

        if (useLifesteal)
            notes.Add($"흡혈: 준 피해의 {Mathf.RoundToInt(lifestealRatio * 100f)}% 회복");

        if (useThorns && thornsDamage > 0)
            notes.Add($"가시 갑옷: 방어도가 있을 때 공격하면 {thornsDamage} 반사");

        if (useEnrage)
            notes.Add(isEnraged
                ? $"분노 중: 공격력 x{enrageAttackMultiplier:0.#}"
                : $"분노: 체력 {Mathf.RoundToInt(enrageHpRatio * 100f)}% 이하면 공격력 x{enrageAttackMultiplier:0.#}");

        if (usePhase2)
        {
            if (isPhase2)
            {
                notes.Add("페이즈 2 진행 중");
            }
            else
            {
                string bonus = phase2Shield > 0 ? $"방어도 +{phase2Shield}" : "";
                if (phase2Heal > 0) bonus += (bonus.Length > 0 ? ", " : "") + $"체력 +{phase2Heal}";
                notes.Add($"페이즈 2: 체력 {Mathf.RoundToInt(phase2HpRatio * 100f)}% 이하면 돌입" +
                          (bonus.Length > 0 ? $" ({bonus})" : ""));
            }
        }

        if (notes.Count > 0)
        {
            sb.Append($"\n<size=22><color={TooltipNoteColor}>");
            foreach (string n in notes) sb.Append('\n').Append(n);
            sb.Append("</color></size>");
        }

        return sb.ToString();
    }

    // ===== 턴 행동 =====

    // ExecuteAction에서 호출: 재생 기믹 → 이번 턴 의도에 맞는 모션과 효과 → 다음 의도 결정 (끝나면 자세를 무조건 원래대로)
    public IEnumerator PlayAttackAnimation()
    {
        if (isActing || hitIsDead) yield break;
        isActing = true;

        BossIntent acted = currentIntent;

        try
        {
            if (useRegen && regenAmount > 0 && hitUnit != null && hitUnit.statData != null &&
                hitUnit.statData.currentHp < hitUnit.statData.maxHp)
            {
                BattleLog.Add($"{hitUnit.LogName} 재생", BattleLog.Kind.Heal);
                hitUnit.Heal(regenAmount);
            }

            Debug.Log($"[{name}] 적 턴 의도: {acted} / 이번 턴 공격력: " +
                      $"{(hitUnit != null ? hitUnit.attackPower.ToString() : "EnemyUnit 없음")}", this);

            switch (acted)
            {
                case BossIntent.Attack:
                case BossIntent.StrongAttack:
                    currentActionStrong = acted == BossIntent.StrongAttack;
                    yield return StartCoroutine(PlayCustomAttack());
                    // 분노 등이 반영된 공격력으로 다시 확정한 뒤 EnemyUnit.Attack으로 피해 (흡혈 OnAttackHit이 여기서 발생)
                    hitUnit.attackPower = GetAttackPower(acted);
                    PlayerUnit target = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
                    if (target != null)
                    {
                        // 돌진 모션의 타격 순간에 이미 흔들었으면 여기서는 다시 흔들지 않음 (HitShake)
                        using (HitShake.PlayerDamageScope())
                            hitUnit.Attack(target);
                    }
                    currentActionStrong = false;
                    break;
                case BossIntent.Defend:
                    yield return StartCoroutine(PlayCustomDefend());
                    break;
                case BossIntent.Charge:
                    yield return StartCoroutine(PlayChargeRoutine());
                    break;
            }

            // 모션 중 분노 등으로 값이 바뀌었어도 공격력만 다시 확정 (방어도는 중복 지급하지 않음)
            if (hitUnit != null) hitUnit.attackPower = GetAttackPower(acted);

            if (!hitIsDead) DecideIntent();
        }
        finally
        {
            RestoreAfterAction();
            isActing = false;
        }
    }

    // 행동이 끝나면 무조건 원래 상태로 (위치 / 크기 / 각도 / 색 / Animator)
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
                allRenderers[i].color = baseColors[i] * persistentTint;
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
        // 강공격(기 모으기 다음 턴)이면 SpecialAttack 클립 + 더 센 흔들림
        bool strong = currentActionStrong;
        string strikeClip = "";
        if (playAttackClip) strikeClip = (strong && HasState(specialAttackState)) ? specialAttackState : attackState;
        float shake = strong ? attackStrikeShake * strongAttackShakeMultiplier : attackStrikeShake;

        float distance = attackDistance;
        if (dashToPlayer && TryGetDashDistanceToPlayer(out float autoDistance)) distance = autoDistance;

        yield return StartCoroutine(DashMotionRoutine(
            distance, attackWindupBack, attackWindupTime, attackDashTime,
            attackStrikeTime, attackReturnTime, attackArcHeight, shake,
            attackLean, attackStrikeScale,
            playMoveClip ? moveState : "", strikeClip));

        // 플레이어 피격 모션이 끝날 때까지 기다림
        if (PlayerSpineController.Instance != null)
        {
            yield return StartCoroutine(PlayerSpineController.Instance.HurtRoutine());
        }

        if (!hitIsDead) ReturnToIdle();    
    }

    // 기 모으기(공격 안 함): 방어 펄스와 구분되게 웅크리며 점점 세게 떨림 + 노랗게 여러 번 번쩍임
    private IEnumerator PlayChargeRoutine()
    {
        if (hitUnit != null) BattleLog.Add($"{hitUnit.LogName}이(가) 힘을 모은다! 다음 턴 강공격");

        if (HasState(chargeState)) animator.CrossFadeInFixedTime(chargeState, crossFadeTime);

        float duration = Mathf.Max(0.1f, chargeDuration);
        int flashes = Mathf.Max(1, chargeFlashCount);
        int lastFlash = -1;
        float t = 0f;

        while (t < duration && !hitIsDead)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            // 처음 30% 동안 웅크리고, 마지막 15%에 펴짐
            float crouch = p < 0.85f ? Mathf.Clamp01(p / 0.3f) : Mathf.Clamp01((1f - p) / 0.15f);
            // 떨림은 점점 세지다가 펴질 때 멈춤
            float shake = p < 0.85f ? chargeShake * p : 0f;
            Vector3 offset = new Vector3(Random.Range(-1f, 1f) * shake, Random.Range(-1f, 1f) * shake * 0.5f, 0f);

            ApplyPose(offset, 0f, 1f + chargeCrouch * crouch, 1f - chargeCrouch * crouch);

            int flash = (int)(p * flashes);
            if (flash != lastFlash && flash < flashes)
            {
                lastFlash = flash;
                StartTint(chargeTintColor, duration / flashes * 0.5f);
            }

            yield return null;
        }

        ResetPose();
        if (!hitIsDead) ReturnToIdle();
    }

    // 방어: 파란 틴트 + 몸이 부푸는 펄스 (+ 있으면 Talk 클립) 뒤에 방어도 획득
    protected override IEnumerator PlayCustomDefend()
    {
        if (guardTintDuration > 0f) StartTint(guardTintColor, guardTintDuration);
        if (HasState(defendState)) animator.CrossFadeInFixedTime(defendState, crossFadeTime);

        if (pulseDuration > 0f)
            yield return StartCoroutine(PulseRoutine());
        else
            yield return new WaitForSeconds(fallbackDuration);

        PlayDefendSound();
        hitUnit.AddShield(defendShield);
        WarnThornsActive();

        if (!hitIsDead) ReturnToIdle();
    }

    // 방어 행동이 아닌 곳(2페이즈 기믹 등)에서 방어도를 얻었을 때도 같은 연출(파란 틴트 + 펄스 + 효과음)
    private void PlayGainShieldFx()
    {
        if (hitIsDead) return;

        if (guardTintDuration > 0f) StartTint(guardTintColor, guardTintDuration);
        PlayDefendSound();

        // 플레이어 턴에 맞다가 발동하므로, 그 사이 보스가 행동을 시작하면 펄스를 멈춰 공격 모션과 겹치지 않게 한다
        if (pulseDuration > 0f) StartCoroutine(PulseRoutine(stopWhenActing: true));
    }

    // 방어 펄스: 몸이 부풀었다 줄어드는 동작을 pulseCount번 (stopWhenActing: 행동이 시작되면 중단)
    private IEnumerator PulseRoutine(bool stopWhenActing = false)
    {
        int count = Mathf.Max(1, pulseCount);
        float t = 0f;

        while (t < pulseDuration && !hitIsDead)
        {
            if (stopWhenActing && isActing) yield break; // 행동 모션이 자세를 이어받으므로 되돌리지 않음

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

    // 공격 돌진 모션: (Move 클립) 뒤로 물러났다가 → 왼쪽(플레이어 쪽)으로 돌진 → 타격(Attack 클립 + 떨림) → 복귀
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
            if (!hitIsDead)
            {
                if (currentActionStrong && strongAttackSound != null) PlayEnemySound(strongAttackSound);
                else PlayAttackSound();
                NotifyAttackImpact(GetAttackPower(currentActionStrong ? BossIntent.StrongAttack : BossIntent.Attack));
            }

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

            ApplyPose(target, lean, 1f, 1f);

            // 공격 클립을 끝까지 보여 준 뒤 잠깐 머물렀다가 복귀 (휘두르는 도중에 뒤로 미끄러지지 않도록)
            if (waitAttackClipBeforeReturn && HasState(strikeClip))
                yield return StartCoroutine(WaitForStateEnd(strikeClip, attackClipEndRatio, t));
            if (attackReturnDelay > 0f && !hitIsDead)
                yield return new WaitForSeconds(attackReturnDelay);

            yield return StartCoroutine(MoveOverTime(target, Vector3.zero, returnTime, 0f, lean, 0f));
        }
        finally
        {
            ResetPose();
        }
    }

    // stateName 상태가 endRatio까지 재생되거나 다른 상태로 넘어갈 때까지 대기 (안전장치: maxActionTime)
    private IEnumerator WaitForStateEnd(string stateName, float endRatio, float alreadyWaited)
    {
        float timer = alreadyWaited;

        // 크로스페이드 중이라 아직 그 상태가 아니면 들어갈 때까지 대기
        while (timer < maxActionTime && !hitIsDead && !animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            timer += Time.deltaTime;
            yield return null;
        }

        while (timer < maxActionTime && !hitIsDead)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName(stateName) || info.normalizedTime >= endRatio) break;

            timer += Time.deltaTime;
            yield return null;
        }
    }

    // from → to로 duration 동안 부드럽게 이동 (arcHeight: 중간에 위로 솟는 높이, tilt: 기울기)
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

            ApplyPose(pos, Mathf.Lerp(tiltFrom, tiltTo, e), 1f, 1f);

            yield return null;
        }

        ApplyPose(to, tiltTo, 1f, 1f);
    }

    // ===== 피격 / 사망 =====

    // 몸 조각이 여러 개인 캐릭터라 모든 조각에 반짝임 (이펙트는 몸 가운데에)
    protected override void PlayHitEffect()
    {
        StartTint(hitFlashColor, hitFlashDuration);

        if (hitEffectPrefab != null)
        {
            Vector3 center = basePos + currentOffset + bodyCenterOffset;
            Instantiate(hitEffectPrefab, center + hitEffectOffset, Quaternion.identity);
        }
    }

    // 모든 몸 조각에 색을 곱했다가(원래 색 × 분노/2페이즈 색 × tint) 일정 시간 뒤 분노/2페이즈 색으로 복원
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
            if (allRenderers[i] != null) allRenderers[i].color = baseColors[i] * persistentTint * tint;
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null) allRenderers[i].color = baseColors[i] * persistentTint;
        }

        tintCoroutine = null;
    }

    // 부모의 공통 반짝임에 사망 / 체력 기믹 / 피격 애니메이션을 덧붙임
    protected override void HandleHpChanged(int current, int max)
    {
        if (hitIsDead) return;

        bool tookDamage = current < hitLastHp;
        bool willDie = current <= 0 && hitUnit != null && hitUnit.statData != null && hitUnit.statData.maxHp > 0;

        base.HandleHpChanged(current, max);

        if (willDie)
        {
            PlayDeath();
            return;
        }

        CheckHpGimmicks(current, max);

        if (tookDamage && !isActing && HasState(hurtState))
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
        if (!isActing && !hitIsDead) ReturnToIdle();
    }

    // 사망: 행동을 막고 처치 연출(설정에 따라) + Death 클립
    private void PlayDeath()
    {
        isActing = true;

        if (intentIcon != null) intentIcon.gameObject.SetActive(false);

        // 보스 처치 연출 (히트스톱 → 슬로모 연쇄 폭발 → 대폭발 + 소멸). 설정: Resources/BossDeathFxSettings
        BossDeathFxSettings deathFx = BossDeathFxSettings.Load();
        if (deathFx.ShouldPlayFor(GameFlowData.currentNodeType))
        {
            BossDeathSequence.Play(gameObject, allRenderers, GetBodyBounds(), deathFx);
        }

        if (HasState(deathState))
        {
            animator.CrossFadeInFixedTime(deathState, crossFadeTime);
            StartCoroutine(FreezeOnDeathEnd());
        }
    }

    // Death 클립이 거의 끝나는 지점에서 멈춰 세움 (다시 일어나지 않도록)
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

    // 이번 의도의 공격력을 EnemyUnit에 반영 (아이콘 숫자와 실제 피해를 맞춤)
    private void ApplyIntentEffect(BossIntent intent)
    {
        if (hitUnit == null) return;

        hitUnit.attackPower = GetAttackPower(intent);
    }

    // 의도 + 분노 상태를 반영한 이번 턴 공격력 (방어 / 기 모으기는 0)
    private int GetAttackPower(BossIntent intent)
    {
        if (intent == BossIntent.Defend || intent == BossIntent.Charge) return 0;

        float power = baseAttackPower;
        if (intent == BossIntent.StrongAttack) power *= chargeMultiplier;
        if (isEnraged) power *= enrageAttackMultiplier;

        return Mathf.Max(0, Mathf.RoundToInt(power));
    }

    // ===== 기믹 =====

    // 체력 비율로 발동하는 기믹 (2페이즈 / 분노, 각각 한 번만)
    private void CheckHpGimmicks(int current, int max)
    {
        if (max <= 0) return;
        float ratio = (float)current / max;

        if (usePhase2 && !isPhase2 && ratio <= phase2HpRatio)
        {
            isPhase2 = true;
            patternIndex = 0;
            persistentTint = phase2TintColor;

            BattleLog.AddHeader($"{hitUnit.LogName}의 페이즈 2!");
            if (phase2Heal > 0) hitUnit.Heal(phase2Heal);
            if (phase2Shield > 0)
            {
                hitUnit.AddShield(phase2Shield);
                WarnThornsActive();
                PlayGainShieldFx();
            }

            ApplyPersistentColors();
        }

        if (useEnrage && !isEnraged && ratio <= enrageHpRatio)
        {
            isEnraged = true;
            if (!isPhase2) persistentTint = enrageTintColor;

            BattleLog.Add($"{hitUnit.LogName}이(가) 분노했다! 공격력 x{enrageAttackMultiplier:0.#}");
            ApplyPersistentColors();
            UpdateIntentIcon(); // 머리 위 공격 수치를 분노 반영 값으로 갱신
        }
    }

    // 분노 / 2페이즈 몸 색 적용 (반짝임 중이면 반짝임이 끝날 때 자동으로 이 색으로 돌아옴)
    private void ApplyPersistentColors()
    {
        if (tintCoroutine != null || allRenderers == null) return;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null) allRenderers[i].color = baseColors[i] * persistentTint;
        }
    }

    // 가시 갑옷: 방어도가 남아 있을 때 맞으면 플레이어에게 반사 피해
    private void HandleDamagedForThorns(int incoming, int shieldBefore)
    {
        if (!useThorns || hitIsDead || incoming <= 0 || shieldBefore <= 0 || thornsDamage <= 0) return;

        PlayerUnit player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (player == null || player.statData == null) return;

        BattleLog.Add($"{hitUnit.LogName}의 가시 갑옷! 플레이어에게 {thornsDamage} 반사", BattleLog.Kind.Damage);
        player.TakeDamage(thornsDamage);
    }

    // 가시 갑옷이 켜진 보스가 방어도를 얻었을 때 전투 로그에 위험 알림
    private void WarnThornsActive()
    {
        if (!useThorns || hitIsDead || hitUnit == null || hitUnit.statData == null || hitUnit.statData.currentShield <= 0) return;

        BattleLog.AddWarning($"가시 갑옷 반사 피해 주의! (방어도가 있는 동안 공격하면 {thornsDamage} 반사)");
    }

    // 흡혈: 플레이어 체력에 실제로 들어간 피해의 일부만큼 회복
    private void HandleAttackHitForLifesteal(int dealt)
    {
        if (!useLifesteal || hitIsDead || dealt <= 0) return;

        int heal = Mathf.Max(1, Mathf.RoundToInt(dealt * lifestealRatio));
        BattleLog.Add($"{hitUnit.LogName} 흡혈", BattleLog.Kind.Heal);
        hitUnit.Heal(heal);
    }

    // ===== Animator 도우미 =====

    // 상태를 재생하고 끝날 때까지 기다림 (상태가 없으면 fallback 시간만큼 대기)
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

    // 상태 이름은 "이름"과 "Base Layer.이름" 두 가지로 확인
    private bool HasState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return false;

        return animator.HasState(0, Animator.StringToHash(stateName))
            || animator.HasState(0, Animator.StringToHash("Base Layer." + stateName));
    }

    // 인스펙터 상태 이름을 실제 Animator 상태 이름으로 맞춘다: 그대로 → 이름 + 접미사 → 클립 이름에서 추측
    private string ResolveState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return stateName;
        if (HasState(stateName)) return stateName;
        if (!string.IsNullOrEmpty(stateSuffix) && HasState(stateName + stateSuffix)) return stateName + stateSuffix;

        if (animator == null || animator.runtimeAnimatorController == null) return stateName;

        // 컨트롤러 안의 클립 이름에서 상태 이름을 추측
        // 예) 클립 "AttackWoodGolem" → "AttackWoodGolem", "AttackoodGolem", ... "AttackGolem" 순서로 상태가 있는지 확인
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

    // 모든 상태 이름을 맞추고, 없는 상태는 경고
    private void ResolveAllStates()
    {
        idleState = ResolveState(idleState);
        attackState = ResolveState(attackState);
        moveState = ResolveState(moveState);
        specialAttackState = ResolveState(specialAttackState);
        defendState = ResolveState(defendState);
        chargeState = ResolveState(chargeState);
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

    // 인스펙터 ⋮ 메뉴에서 실행: 이 보스 Animator에 들어 있는 클립 이름 확인용
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