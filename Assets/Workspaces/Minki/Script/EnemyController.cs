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

    protected virtual void Start()
    {
        currentHp = maxHp;
        originalPosition = transform.position;
        
        // 게임 시작 시 무작위로 첫 의도(공격 또는 방어)를 결정하고 아이콘 띄우기
        isNextAttack = (Random.value > 0.5f);
        UpdateIntentUI();
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

    // 데미지를 입는 공통 함수 (외부에서 호출 가능)
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