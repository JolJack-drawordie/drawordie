using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public Animator animator;
<<<<<<< Updated upstream
    public Transform attackTarget; // 적 오브젝트 연결 (비워두면 고정 5칸 이동)
=======
    public GameObject potionObject; 
>>>>>>> Stashed changes
    private Vector3 originalPosition;
    private bool isActing = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        originalPosition = transform.position;
        
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Defend");

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

        // 3키: 포션 마시기
        if (Input.GetKeyDown(KeyCode.Alpha3) && !isActing)
        {
            StartCoroutine(PotionRoutine());
        }
    }

<<<<<<< Updated upstream
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
=======
    IEnumerator AttackRoutine()
    {
        isActing = true;
        Vector3 targetPos = originalPosition + new Vector3(5f, 0, 0);
        float timer = 0;
>>>>>>> Stashed changes
        
        while (timer <= 0.15f)
        {
            transform.position = Vector3.Lerp(originalPosition, targetPos, timer / 0.15f);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
        
<<<<<<< Updated upstream
        // 2️⃣ 도착하면 공격 애니메이션 시작
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(1.2f);

        // 3️⃣ 천천히 돌아오기
        timer = 0;
        float returningDuration = 0.6f;
        
        while (timer <= returningDuration)
=======
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.9f);
        
        timer = 0;
        while (timer <= 0.4f)
>>>>>>> Stashed changes
        {
            transform.position = Vector3.Lerp(targetPos, originalPosition, timer / 0.4f);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
        isActing = false;
    }

<<<<<<< Updated upstream
    // 방어 동작: 제자리에서 방어
    public IEnumerator DefendRoutine()
=======
    IEnumerator DefendRoutine()
>>>>>>> Stashed changes
    {
        isActing = true;
        animator.SetTrigger("Defend");
        yield return new WaitForSeconds(0.8f);
        isActing = false;
    }

    IEnumerator PotionRoutine()
    {
        isActing = true;
        GameObject potionObj = potionObject; 

        if (potionObj != null)
        {
            SpriteRenderer sr = potionObj.GetComponent<SpriteRenderer>();
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
                    c.a = Mathf.Lerp(0, 1, progress * 2f); 
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