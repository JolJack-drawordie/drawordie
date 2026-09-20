using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossController : EnemyController
{
    private Animator animator;
    private EnemyUnit unit;        // 실제 체력/쉴드/공격력을 관리하는 유닛
    private int baseAttackPower;   // 프리팹에 설정된 기본 공격력

    [Header("보스 전용 스프라이트")]
    public Sprite spellSprite; // 스펠 의도 아이콘

    [Header("스폰 위치 보정 (스폰 포인트 기준으로 이동)")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("보스 행동 수치")]
    public int defendShield = 5; // 방어(Cast) 시 얻는 방어도
    public int spellDamage = 8;  // 스펠 시 플레이어에게 주는 데미지

    [Header("애니메이션 종료 대기 설정")]
    public string idleStateName = "Idle"; // Animator의 기본 상태 이름
    public float maxActionTime = 5.0f;    // 애니메이션이 안 끝나는 경우를 대비한 안전장치

    [Header("단독 테스트용 (실제 전투에서는 꺼 두기)")]
    public bool useTestKey = false;
    public KeyCode testKey = KeyCode.Alpha3;
    public float actionDelay = 2.0f;

    // 애니메이터 Trigger 이름 (Animator Controller의 파라미터 이름과 동일해야 함)
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int CastHash = Animator.StringToHash("Cast");   // 방어
    private static readonly int SpellHash = Animator.StringToHash("Spell");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    private enum BossIntent { Attack = 0, Defend = 1, Spell = 2 }
    private BossIntent currentIntent = BossIntent.Attack;

    protected override void Start()
    {
        // 스폰 포인트 위치에서 보스만 따로 위치 보정 (base.Start 전에 적용해야 originalPosition이 맞음)
        transform.position += spawnOffset;

        base.Start();

        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("보스에서 Animator를 찾지 못했습니다!", this);
        }
        else
        {
            // 파라미터 기본값이 켜져 있어도 시작하자마자 자동 재생되지 않도록 초기화
            animator.ResetTrigger(AttackHash);
            animator.ResetTrigger(CastHash);
            animator.ResetTrigger(SpellHash);
            animator.ResetTrigger(HurtHash);
        }

        unit = GetComponent<EnemyUnit>();
        if (unit != null) baseAttackPower = unit.attackPower;
        else Debug.LogWarning("EnemyUnit이 없어 단독 테스트 모드로 동작합니다. (방어/스펠 효과는 적용되지 않음. 실제 전투용 프리팹에는 EnemyUnit이 필요합니다)", this);

        // 첫 의도 결정 및 UI 갱신
        DecideBossIntent();
    }

    // 부모의 Update(스케일 꿀렁임)를 완전히 대체: 보스는 애니메이션만 사용
    protected override void Update()
    {
        // 머리 위 캔버스 위치 추적
        if (intentCanvas != null)
        {
            intentCanvas.transform.position = transform.position + new Vector3(0, 2.0f, 0);
        }

        // 단독 테스트용 키 입력
        if (useTestKey && Input.GetKeyDown(testKey))
        {
            ExecuteAction();
        }
    }

    // TurnManager가 적 턴에 호출하는 함수 (이 코루틴이 끝나면 TurnManager가 enemy.Attack을 호출함)
    public override IEnumerator PlayAttackAnimation()
    {
        if (isActing) yield break;
        yield return StartCoroutine(PerformIntent(0f));
    }

    // 단독 테스트용 실행 (딜레이 포함)
    public override void ExecuteAction()
    {
        if (isActing) return;
        StartCoroutine(PerformIntent(actionDelay));
    }

    // 공통 행동 루틴: (딜레이) → 효과 적용 → 애니메이션 → 종료 대기 → 다음 의도 결정(아이콘 갱신)
    private IEnumerator PerformIntent(float delay)
    {
        isActing = true;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        ApplyIntentEffect();

        if (animator != null)
        {
            animator.SetTrigger(GetTriggerHash(currentIntent));
            yield return StartCoroutine(WaitForAnimationEnd());
        }

        // 애니메이션이 완전히 끝난 뒤에 아이콘 갱신
        DecideBossIntent();
        isActing = false;
    }

    // TurnManager가 애니메이션 직후 enemy.Attack()을 호출하므로,
    // 의도에 따라 공격력을 바꿔 두는 방식으로 팀 코드를 수정하지 않고 처리
    private void ApplyIntentEffect()
    {
        if (unit == null) return;

        switch (currentIntent)
        {
            case BossIntent.Attack:
                unit.attackPower = baseAttackPower;
                break;
            case BossIntent.Defend:
                unit.attackPower = 0;          // 이번 턴은 공격하지 않음
                unit.AddShield(defendShield);
                break;
            case BossIntent.Spell:
                unit.attackPower = spellDamage;
                break;
        }
    }

    private int GetTriggerHash(BossIntent intent)
    {
        switch (intent)
        {
            case BossIntent.Defend: return CastHash;
            case BossIntent.Spell: return SpellHash;
            default: return AttackHash;
        }
    }

    // Idle을 벗어났다가 다시 Idle로 돌아올 때까지 대기
    private IEnumerator WaitForAnimationEnd()
    {
        float timer = 0f;

        while (IsInIdle() && timer < maxActionTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        while (!IsInIdle() && timer < maxActionTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private bool IsInIdle()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(idleStateName);
    }

    // 다음 의도를 3가지 중 랜덤 결정
    private void DecideBossIntent()
    {
        currentIntent = (BossIntent)Random.Range(0, 3);
        isNextAttack = (currentIntent == BossIntent.Attack);
        UpdateBossIntentUI();
    }

    // 머리 위 아이콘 갱신 (공격, 방어, 스펠)
    private void UpdateBossIntentUI()
    {
        if (intentIcon == null) return;

        switch (currentIntent)
        {
            case BossIntent.Attack:
                intentIcon.sprite = attackSprite;
                break;
            case BossIntent.Defend:
                intentIcon.sprite = defendSprite;
                break;
            case BossIntent.Spell:
                if (spellSprite != null) intentIcon.sprite = spellSprite;
                break;
        }
    }

    // 부모 클래스의 추상 메서드 구현용 (보스는 애니메이션만 쓰므로 비워 둠)
    protected override IEnumerator PlayCustomAttack() { yield break; }
    protected override IEnumerator PlayCustomDefend() { yield break; }
}