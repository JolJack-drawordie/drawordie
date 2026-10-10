using UnityEngine;
using System.Collections;
using Spine.Unity; // 스파인 사용을 위해 필수

public class PlayerSpineController : MonoBehaviour
{
    public static PlayerSpineController Instance; // 전역 접근용

    [Header("Spine Components")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    [SerializeField] private string idleAnim = "idle";
    [SerializeField] private string runAnim = "run";       // 달리기 모션
    [SerializeField] private string hurtAnim = "hurt";      // 피격 모션
    [SerializeField] private string attackAnim1 = "attack1";
    [SerializeField] private string attackAnim2 = "attack2";
    [SerializeField] private string attackAnim3 = "attack3"; //방어 모션 
    [SerializeField] private string defeatAnim = "defeat";  // 추가: 패배/사망 모션
    [SerializeField] private string chargeAnim = "charge";  // 추가: 회복 모션

    [Header("Movement & Combat Settings")]
    public Transform attackTarget; // 적 오브젝트 연결 (비워두면 고정 5칸 이동)
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

        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    void Start()
    {
        originalPosition = transform.position;
        PlayIdle();
    }

    void Update()
    {
        // 1키: 첫 번째 공격
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isActing)
        {
            StartCoroutine(AttackRoutine(attackAnim1));
        }

        // 2키: 두 번째 공격
        if (Input.GetKeyDown(KeyCode.Alpha2) && !isActing)
        {
            StartCoroutine(AttackRoutine(attackAnim2));
        }

        // 3키: 세 번째 공격
        if (Input.GetKeyDown(KeyCode.Alpha3) && !isActing)
        {
            StartCoroutine(AttackRoutine(attackAnim3));
        }

        // 4키: 피격 테스트
        if (Input.GetKeyDown(KeyCode.Alpha4) && !isActing)
        {
            StartCoroutine(HurtRoutine());
        }
    }

    /// <summary>
    /// 기본 대기 상태 (무한 반복)
    /// </summary>
    public void PlayIdle()
    {
        if (skeletonAnimation != null)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, idleAnim, true);
        }
    }

    /// <summary>
    /// 공통 공격 코루틴 (이동 + 스파인 애니메이션 제어)
    /// </summary>
    public IEnumerator AttackRoutine(string animName)
    {
        isActing = true;

        // 1. 공격하러 달려갈 때 run 모션 재생 (무한 반복)
        skeletonAnimation.AnimationState.SetAnimation(0, runAnim, true);

        // attackTarget이 있으면 적 위치까지, 없으면 고정 5칸 이동
        Vector3 targetPos = attackTarget != null
            ? new Vector3(attackTarget.position.x - 4f, originalPosition.y, originalPosition.z)
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

        // 2. 도착하면 전달받은 스파인 공격 애니메이션 실행 (Loop = false)
        skeletonAnimation.AnimationState.SetAnimation(0, animName, false);
        skeletonAnimation.AnimationState.AddAnimation(0, idleAnim, true, 0f);

        if (SoundManager.Instance != null && SoundManager.Instance.hitSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.hitSound);
        }

        // 애니메이션 재생 시간 동안 대기
        yield return new WaitForSeconds(1.0f);

        // 3. 제자리로 돌아올 때 다시 run 모션 재생 (무한 반복)
        skeletonAnimation.AnimationState.SetAnimation(0, runAnim, true);

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

        // 제자리 복귀 후 대기 모션으로 전환
        PlayIdle();
        isActing = false;
    }

    public IEnumerator HurtRoutine()
    {
        if (skeletonAnimation == null) yield break;

        if (SoundManager.Instance != null && SoundManager.Instance.hitSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.hitSound);
        }

        // 기존 트랙 0번에 예약된 명령 제거 후 피격 애니메이션 즉시 실행
        skeletonAnimation.AnimationState.ClearTrack(0);
        skeletonAnimation.AnimationState.SetAnimation(0, hurtAnim, false);
        skeletonAnimation.AnimationState.AddAnimation(0, idleAnim, true, 0f);

        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 기모으기 / 버프 사용 시 호출하는 함수
    /// </summary>
    public IEnumerator HealRoutine(float duration = 1.0f, bool loop = false)
    {
        if (skeletonAnimation == null) yield break;

        if (SoundManager.Instance != null && SoundManager.Instance.healSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.healSound);
        }

        isActing = true;
        skeletonAnimation.AnimationState.ClearTrack(0);

        // heal 모션 재생 (loop 여부에 따라 반복 설정 가능)
        skeletonAnimation.AnimationState.SetAnimation(0, chargeAnim, loop);

        yield return new WaitForSeconds(duration);

        // 끝나면 idle로 복귀
        PlayIdle();
        isActing = false;
    }

    /// <summary>
    /// 패배(사망) 시 호출하는 함수 (보통 체력 0이 되었을 때 재생 후 멈춤)
    /// </summary>
    public void PlayDefeat()
    {
        if (skeletonAnimation == null) return;

        isActing = true;
        skeletonAnimation.AnimationState.ClearTrack(0);

        // 패배 모션은 보통 마지막 프레임에 멈춰있어야 하므로 loop = false 설정
        skeletonAnimation.AnimationState.SetAnimation(0, defeatAnim, false);
    }

    public IEnumerator ShieldRoutine(float duration = 1.0f)
    {
        if (skeletonAnimation == null) yield break;

        isActing = true;

        if (SoundManager.Instance != null && SoundManager.Instance.shieldSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.shieldSound);
        }

        // 기존 트랙 비우고 방어 애니메이션 즉시 실행 (loop = false, 끝나면 idle 복귀)
        skeletonAnimation.AnimationState.ClearTrack(0);
        skeletonAnimation.AnimationState.SetAnimation(0, attackAnim3, false);
        skeletonAnimation.AnimationState.AddAnimation(0, idleAnim, true, 0f);

        // 방어 모션 유지 시간 동안 대기
        yield return new WaitForSeconds(duration);

        PlayIdle();
        isActing = false;
    }
}