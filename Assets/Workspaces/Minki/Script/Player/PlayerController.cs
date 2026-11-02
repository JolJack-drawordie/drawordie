using UnityEngine;

// 휴식 씬 플레이어(Player 프리팹)의 시작 상태 정리. 예전 테스트용 공격 / 방어 / 포션 동작은 지웠다
public class PlayerController : MonoBehaviour
{
    public Animator animator;
    public GameObject potionObject; // 포션 연출용 오브젝트 (처음엔 숨김)

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // 시작하자마자 공격 / 방어 애니메이션이 재생되지 않도록 트리거 초기화
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Defend");

        // 포션 오브젝트는 숨겨 둠
        if (potionObject != null)
            potionObject.SetActive(false);
    }
}
