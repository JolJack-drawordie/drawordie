using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public Animator animator;
    private Vector3 originalPosition;
    private bool isActing = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        
        originalPosition = transform.position;
        
        // 게임 시작 시 자동 공격/방어 방지
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Defend");
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
    }

    // 공격 동작: 빠르게 달려나가서 2번 휘두르고 천천히 뒤로 물러나기
    IEnumerator AttackRoutine()
    {
        isActing = true;
        
        // 1️⃣ 빠르게 달려나가기 (0.15초, 5칸)
        Vector3 targetPos = originalPosition + new Vector3(5f, 0, 0);
        float timer = 0;
        float goingDuration = 0.15f;
        
        while (timer <= goingDuration)
        {
            float progress = timer / goingDuration;
            transform.position = Vector3.Lerp(originalPosition, targetPos, progress);
            timer += Time.deltaTime;
            yield return null;
        }
        
        transform.position = targetPos;
        
        // 2️⃣ 도착하면 공격 애니메이션 시작 (2번 휘두르기, 0.6초)
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.9f);
        
        // 3️⃣ 천천히 돌아오기 (0.4초)
        timer = 0;
        float returningDuration = 0.4f;
        
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
    IEnumerator DefendRoutine()
    {
        isActing = true;
        animator.SetTrigger("Defend");

        // 방어 애니메이션 재생 (약 0.8초)
        yield return new WaitForSeconds(0.8f);

        isActing = false;
    }
}