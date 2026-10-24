using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class EnemyController : MonoBehaviour
{
    [Header("체력 설정")]
    public int maxHp = 20;
    protected int currentHp;
    public UnitBase unitData;

    [Header("의도(Intent) UI")]
    public Image intentIcon; 
    public Sprite attackSprite; 
    public Sprite defendSprite; 
    protected bool isNextAttack = true; // 다음 행동 의도 (true: 공격, false: 방어)
    public int currentAttack;
    public int currentDefend;
    public TextMeshProUGUI actionText;
    protected AttackData selectedAttack; //선택된 공격패턴

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

    [Header("몸 표시 보정 (전투 배경에 가려지지 않도록)")]
    [Tooltip("몸 조각들의 정렬 순서를 최소 이 값 이상으로 올림 (전투 배경 BackgroundFloor가 2). 0 이하면 사용 안 함")]
    public int minBodySortingOrder = 10;

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
        actionText = GetComponentInChildren<TextMeshProUGUI>();
        unitData = GetComponentInChildren<UnitBase>();

        // 게임 시작 시 무작위로 첫 의도(공격 또는 방어)를 결정하고 아이콘 띄우기
        DecideNextAction();

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

    // 에셋 프리팹을 자식으로 넣으면 따라오는 문제를 자동 보정
    // 1) 자식의 Z 위치가 -75 같은 값이면 카메라(Z -10) 뒤로 가서 안 보임 -> Z를 0으로
    // 2) 몸 조각 정렬 순서가 0~2면 전투 배경(BackgroundFloor 2) 뒤에 가려짐 -> 상대 순서는 유지한 채 위로 올림
    protected void PrepareBodyRenderers(SpriteRenderer[] renderers)
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Canvas>() != null) continue;
            if (Mathf.Abs(child.localPosition.z) > 1f)
            {
                Debug.LogWarning($"[{name}] 자식 '{child.name}'의 Z 위치({child.localPosition.z})가 카메라 뒤로 갈 수 있어 0으로 맞춥니다. 프리팹에서 0으로 바꿔두세요.", this);
                Vector3 p = child.localPosition;
                p.z = 0f;
                child.localPosition = p;
            }
        }

        if (renderers == null || renderers.Length == 0 || minBodySortingOrder <= 0) return;

        int lowest = int.MaxValue;
        foreach (SpriteRenderer r in renderers)
        {
            if (r != null && r.sortingOrder < lowest) lowest = r.sortingOrder;
        }

        if (lowest >= minBodySortingOrder) return;

        int shift = minBodySortingOrder - lowest;
        foreach (SpriteRenderer r in renderers)
        {
            if (r != null) r.sortingOrder += shift;
        }
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
    public virtual IEnumerator ExecuteAction()
    {
        if (isNextAttack)
        {
            yield return StartCoroutine(AttackRoutineWrapper());
        }
        else
        {
            yield return StartCoroutine(DefendRoutineWrapper());
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

        DecideNextAction();

        GameManager.Instance.currentPlayer.TakeDamage(currentAttack);

        isActing = false;
    }

    // 방어 모션 수행 래퍼
    IEnumerator DefendRoutineWrapper()
    {
        isActing = true;
        yield return StartCoroutine(PlayCustomDefend());

        DecideNextAction();

        hitUnit.AddShield(currentDefend);

        isActing = false;
    }

    protected void DecideNextAction()
    {
        // 1. 다음 행동을 공격(true) 또는 방어(false)로 무작위 결정
        isNextAttack = (Random.value > 0.5f);
        Debug.Log($"[디버깅] Decide 진입! 다음 행동은 공격인가? {isNextAttack}"); 

        // 2. unitData나 statData가 안전하게 있는지 체크
        if (unitData != null && unitData.statData != null)
        {
            if (isNextAttack)
            {
                // 공격일 경우: 리스트에서 무작위 스킬 하나 뽑아서 데미지 세팅
                if (unitData.statData.attacks != null && unitData.statData.attacks.Count > 0)
                {
                    int randomIndex = Random.Range(0, unitData.statData.attacks.Count);
                    selectedAttack = unitData.statData.attacks[randomIndex];
                    currentAttack = selectedAttack.damage;
                    Debug.Log($"[디버깅] 공격 스킬 뽑음! 이름: {selectedAttack.name}, 데미지: {currentAttack}"); 
                }
                else
                {
                    Debug.LogWarning("[디버깅 경고] attacks 리스트가 비어있거나 null이야!"); 
                }

                if (actionText != null)
                {
                    actionText.text = currentAttack.ToString();
                }
            }
            else
            {
                // 방어일 경우: 방어력 세팅
                currentDefend = unitData.statData.defend;

                if (actionText != null)
                {
                    actionText.text = currentDefend.ToString();
                    Debug.Log($"[디버깅] 방어력 텍스트 갱신: {currentDefend}"); 
                }
            }
        }

        // 3. UI 아이콘 갱신
        UpdateIntentUI();
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