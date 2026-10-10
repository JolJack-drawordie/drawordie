using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class EnemyController : MonoBehaviour
{
    [Header("체력 설정")]
    public int maxHp = 30;
    protected int currentHp;

    [Header("의도(Intent) UI")]
    public Image intentIcon; 
    public Sprite attackSprite; 
    public Sprite defendSprite; 
    protected bool isNextAttack = true; // 다음 행동 의도 (true: 공격, false: 방어)

    [Header("공통 연출 설정")]
    public float baseScale = 2f;
    protected Vector3 originalPosition;
    protected bool isActing = false;

    [Header("월드 스페이스 UI 추적 설정")]
    public Canvas intentCanvas; // 독립시킨 월드 스페이스 캔버스 연결용

    [Header("피격 이펙트 (공통)")]
    public Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);   // 맞았을 때 순간적으로 바뀌는 색
    public float hitFlashDuration = 0.1f;       // 색이 유지되는 시간
    public GameObject hitEffectPrefab;          // 선택 사항: 타격 파티클/스프라이트 프리팹 (비워두면 색 반짝임만 재생)
    public Vector3 hitEffectOffset = Vector3.zero;

    // 실제 체력을 관리하는 유닛 (전투 씬에서 BattleFactory가 스폰한 프리팹에 붙어 있음)
    protected EnemyUnit hitUnit;
    protected bool hitIsDead = false;
    protected int hitLastHp;

    private SpriteRenderer hitRenderer;
    private Coroutine hitFlashCoroutine;

    protected virtual void Start()
    {
        currentHp = maxHp;
        originalPosition = transform.position;
        
        // 게임 시작 시 무작위로 첫 의도(공격 또는 방어)를 결정하고 아이콘 띄우기
        isNextAttack = (Random.value > 0.5f);
        UpdateIntentUI();

        // ----- 피격 이펙트 공통 설정 -----
        hitRenderer = GetComponent<SpriteRenderer>();
        if (hitRenderer == null) hitRenderer = GetComponentInChildren<SpriteRenderer>();

        hitUnit = GetComponent<EnemyUnit>();
        if (hitUnit != null)
        {
            hitLastHp = hitUnit.statData != null ? hitUnit.statData.currentHp : 0;
            hitUnit.OnHpChanged += HandleHpChanged;
        }
    }

    protected virtual void Update()
    {
        // 행동 중이 아닐 때 생동감을 주는 꿀렁임(숨쉬기) 연출
        if (!isActing)
        {
            float bounce = Mathf.Sin(Time.time * 2f) * 0.05f;
            transform.localScale = new Vector3(baseScale + bounce, baseScale - bounce, baseScale);
        }

        // 캔버스가 몬스터의 머리 위 좌표를 매 프레임 부드럽게 추적 (위치 어긋남 방지)
        if (intentCanvas != null)
        {
            intentCanvas.transform.position = transform.position + new Vector3(0, 2.0f, 0);
        }
    }

    private void OnDestroy()
    {
        if (hitUnit != null) hitUnit.OnHpChanged -= HandleHpChanged;
    }

    // 실제 체력(EnemyUnit)이 바뀔 때마다 호출됨. 자식 클래스(보스 등)에서 override해서
    // 추가 연출(애니메이션 등)을 덧붙일 수 있음 (base.HandleHpChanged 먼저 호출해서 공통 반짝임 재생 권장)
    protected virtual void HandleHpChanged(int current, int max)
    {
        if (hitIsDead) return;

        bool tookDamage = current < hitLastHp;

        if (tookDamage)
        {
            PlayHitEffect();
        }

        if (current <= 0)
        {
            hitIsDead = true;
        }

        hitLastHp = current;
    }

    // 맞는 순간 재생되는 공통 시각 효과: 스프라이트 색 반짝임 + (있다면) 이펙트 프리팹 생성
    protected virtual void PlayHitEffect()
    {
        if (hitRenderer != null)
        {
            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(FlashRoutine());
        }

        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position + hitEffectOffset, Quaternion.identity);
        }
    }

    private IEnumerator FlashRoutine()
    {
        // 맞기 직전 색을 저장했다가 복원 (고스트처럼 알파값이 계속 바뀌는 연출과 충돌하지 않도록)
        Color before = hitRenderer.color;
        hitRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        hitRenderer.color = before;
    }

    // 데미지를 입는 공통 함수 (외부에서 호출 가능) - 기존 레거시 로직 그대로 유지
    public virtual void TakeDamage(int damage)
    {
        currentHp -= damage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        
        Debug.Log($"{gameObject.name}이(가) {damage}의 데미지를 입었습니다. 남은 체력: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public virtual IEnumerator PlayAttackAnimation()
    {
        yield return StartCoroutine(PlayCustomAttack());
    }
    // 사망 처리
    protected virtual void Die()
    {
        // 사망 시 머리 위에 떠 있던 캔버스도 함께 제거
        if (intentCanvas != null)
        {
            Destroy(intentCanvas.gameObject);
        }

        Debug.Log($"{gameObject.name}이(가) 사망했습니다.");
        Destroy(gameObject);
    }

    // 외부(또는 턴 매니저)에서 호출할 공통 행동 실행 함수
    public virtual void ExecuteAction()
    {
        if (isNextAttack)
        {
            StartCoroutine(AttackRoutineWrapper());
        }
        else
        {
            StartCoroutine(DefendRoutineWrapper());
        }
    }

    // 공격 모션 수행 래퍼
    IEnumerator AttackRoutineWrapper()
    {
        isActing = true;
        yield return StartCoroutine(PlayCustomAttack());

        // 플레이어 피격 처리 (OnHit 실행 및 피격 완료까지 대기)
        if (PlayerSpineController.Instance != null)
        {
            // OnHit 코루틴을 yield return으로 받아서 피격 모션(0.5초 등)이 끝날 때까지 기다림
            yield return StartCoroutine(PlayerSpineController.Instance.HurtRoutine());
        }

        isNextAttack = (Random.value > 0.5f); 
        UpdateIntentUI();
        
        isActing = false;
    }

    // 방어 모션 수행 래퍼
    IEnumerator DefendRoutineWrapper()
    {
        isActing = true;
        yield return StartCoroutine(PlayCustomDefend()); 
        
        isNextAttack = (Random.value > 0.5f); 
        UpdateIntentUI();
        
        isActing = false;
    }

    // 의도 아이콘 업데이트
    protected void UpdateIntentUI()
    {
        if (intentIcon == null) return;

        if (isNextAttack) 
        {
            intentIcon.sprite = attackSprite;
        }
        else 
        {
            intentIcon.sprite = defendSprite;
        }
    }

    // 몬스터마다 모션이 다르므로 자식 스크립트에서 구현(override)
    protected abstract IEnumerator PlayCustomAttack();
    protected abstract IEnumerator PlayCustomDefend();
}