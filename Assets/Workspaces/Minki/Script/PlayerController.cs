using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance; // 전역 접근용

    public Animator animator;
    public Transform attackTarget; // 적 오브젝트 연결 (비워두면 고정 5칸 이동)
    public GameObject potionObject; // 포션 오브젝트 연결 (추가)
    private Vector3 originalPosition;
    private bool isActing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        originalPosition = transform.position;
        
        // 게임 시작 시 자동 공격/방어 방지
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Defend");

        // 포션 오브젝트 비활성화 (추가)
        if (potionObject != null)
            potionObject.SetActive(false);
    }

    void Update()
    {
        // 1키: 공격
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isActing)
        {
            StartCoroutine(AttackRoutine());
        }

        // 2키: 방어
        if (Input.GetKeyDown(KeyCode.Alpha2) && !isActing)
        {
            StartCoroutine(DefendRoutine());
        }

        // 3키: 포션 마시기 (추가)
        if (Input.GetKeyDown(KeyCode.Alpha3) && !isActing)
        {
            StartCoroutine(PotionRoutine());
        }
    }

    // 공격 동작: 빠르게 달려나가서 2번 휘두르고 천천히 뒤로 물러나기
    public IEnumerator AttackRoutine()
    {
        isActing = true;

        // attackTarget이 있으면 적 위치까지, 없으면 고정 5칸
        Vector3 targetPos = attackTarget != null
            ? new Vector3(attackTarget.position.x - 1f, originalPosition.y, originalPosition.z)
            : originalPosition + new Vector3(5f, 0, 0);

        float timer = 0;
        float goingDuration = 0.25f;
        
        while (timer <= goingDuration)
        {
            float progress = timer / goingDuration;
            transform.position = Vector3.Lerp(originalPosition, targetPos, progress);
            timer += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPos;
        
        // 2️⃣ 도착하면 공격 애니메이션 시작
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(1.2f);

        // 3️⃣ 천천히 돌아오기
        timer = 0;
        float returningDuration = 0.6f;
        
        while (timer <= returningDuration)
        {
            float progress = timer / returningDuration;
            transform.position = Vector3.Lerp(targetPos, originalPosition, progress);
            timer += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
        isActing = false;
    }

    // 방어 동작: 제자리에서 방어
    public IEnumerator DefendRoutine()
    {
        isActing = true;
        animator.SetTrigger("Defend");

        // 방어 애니메이션 재생 (약 0.8초)
        yield return new WaitForSeconds(0.8f);

        isActing = false;
    }

    // 포션 마시기 동작 (추가)
    IEnumerator PotionRoutine()
    {
        isActing = true;
        GameObject potionObj = potionObject; 

        if (potionObj != null)
        {
            SpriteRenderer sr = potionObj.GetComponent<SpriteRenderer>();
            // 초기 위치 및 투명도 설정
            potionObj.transform.localPosition = new Vector3(0.7f, -0.5f, 0); 
            potionObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            potionObj.transform.localScale = Vector3.one * 0.7f;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0;
                sr.color = c;
            }
            potionObj.SetActive(true);
        }

        float duration = 0.8f;
        float timer = 0;

        while (timer <= duration)
        {
            float progress = timer / duration; 

            if (potionObj != null)
            {
                SpriteRenderer sr = potionObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(0, 1, progress * 2f); // 서서히 나타남
                    sr.color = c;
                }

                potionObj.transform.localPosition = new Vector3(0.7f, -0.5f, 0);
                float currentZ = Mathf.Lerp(0f, 30f, progress);
                potionObj.transform.localRotation = Quaternion.Euler(0, 0, currentZ);
                float currentScale = Mathf.Lerp(0.7f, 1.0f, progress);
                potionObj.transform.localScale = Vector3.one * currentScale;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (potionObj != null)
        {
            potionObj.SetActive(false);
        }

        Debug.Log("포션을 마셔 체력을 회복했습니다!");
        isActing = false;
    }
}