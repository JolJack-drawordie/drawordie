using UnityEngine;
using System.Collections;
using Spine.Unity;

// 전투 플레이어(Death Knight, Spine) 모션: 대기, 공격(일반 1회 / 조합 카드 2회 연속), 피격, 방어, 회복, 패배.
// 공격 모션의 타격 순간마다 OnAttackImpact를 알려 화면 흔들림(HitShake)을 맞춘다.
public class PlayerSpineController : MonoBehaviour
{
    public static PlayerSpineController Instance;

    [Header("Spine Components")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    [SerializeField] private string idleAnim = "idle";
    [SerializeField] private string runAnim = "run"; // 적에게 달려갈 때 / 돌아올 때
    [SerializeField] private string hurtAnim = "hurt"; // 피격
    [SerializeField] private string attackAnim1 = "attack1";
    [SerializeField] private string attackAnim2 = "attack2";
    [SerializeField] private string attackAnim3 = "attack3"; // 방어 모션으로 사용
    [SerializeField] private string defeatAnim = "defeat"; // 패배 (마지막 프레임에서 멈춤)
    [SerializeField] private string chargeAnim = "charge"; // 회복 모션으로 사용

    [Header("Movement & Combat Settings")]
    public Transform attackTarget; // 공격할 적 (BattleFactory가 연결, 비어 있으면 앞으로 5칸 이동)
    private Vector3 originalPosition;

    // 공격 모션에서 칼이 적에게 닿는 시각 (attack1의 slash 이펙트가 뜨는 0.2초). 화면 흔들림(HitShake) 타이밍에 사용
    [SerializeField] private float attackImpactTime = 0.2f;
    public static event System.Action OnAttackImpact;

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

    // 기본 대기 모션 (반복)
    public void PlayIdle()
    {
        if (skeletonAnimation != null)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, idleAnim, true);
        }
    }

    // 일반 공격: 적 앞으로 달려가서 공격 모션 1회 후 복귀
    public IEnumerator AttackRoutine(string animName)
    {
        yield return AttackSequenceRoutine(new[] { animName });
    }

    // 조합 카드 공격: 적 앞으로 달려가서 그 자리에서 공격 모션 2개(attack1 → attack2)를 이어서 보여 준 뒤 복귀
    public IEnumerator ComboAttackRoutine()
    {
        yield return AttackSequenceRoutine(new[] { attackAnim1, attackAnim2 });
    }

    // 이동 → 공격 모션들을 차례로 재생(각 모션의 타격 순간마다 OnAttackImpact) → 복귀
    private IEnumerator AttackSequenceRoutine(string[] animNames)
    {
        // 1. 적에게 달려갈 때 run 모션 (반복)
        skeletonAnimation.AnimationState.SetAnimation(0, runAnim, true);

        // 적 앞 4칸까지 (적이 없으면 앞으로 5칸)
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

        // 2. 도착한 자리에서 공격 모션을 하나씩 재생 (반복 없음). 마지막 모션 뒤에만 대기 모션 예약
        float impactTime = Mathf.Clamp(attackImpactTime, 0f, 1.0f);
        for (int i = 0; i < animNames.Length; i++)
        {
            bool isLast = i == animNames.Length - 1;

            skeletonAnimation.AnimationState.SetAnimation(0, animNames[i], false);
            if (isLast) skeletonAnimation.AnimationState.AddAnimation(0, idleAnim, true, 0f);

            PlayAttackSound();

            // 타격 순간을 알린 뒤, 다음 모션이 있으면 이번 모션이 끝날 때까지 / 마지막이면 1초를 채울 때까지 대기
            yield return new WaitForSeconds(impactTime);
            OnAttackImpact?.Invoke();

            float rest = isLast ? 1.0f - impactTime : Mathf.Max(0f, GetAnimationDuration(animNames[i]) - impactTime);
            yield return new WaitForSeconds(rest);
        }

        // 3. 제자리로 돌아올 때 다시 run 모션 (반복)
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

        // 제자리로 돌아오면 대기 모션
        PlayIdle();
    }

    // 공격 효과음 (전용 소리가 없으면 피격음)
    private void PlayAttackSound()
    {
        if (SoundManager.Instance == null) return;

        AudioClip attackClip = SoundManager.Instance.playerAttackSound != null ? SoundManager.Instance.playerAttackSound : SoundManager.Instance.hitSound;
        if (attackClip != null) SoundManager.Instance.PlaySFX(attackClip);
    }

    // Spine 애니메이션 길이 (못 찾으면 공격 모션 기본 길이 0.833초)
    private float GetAnimationDuration(string animName)
    {
        var anim = skeletonAnimation != null && skeletonAnimation.Skeleton != null
            ? skeletonAnimation.Skeleton.Data.FindAnimation(animName)
            : null;
        return anim != null ? anim.Duration : 0.833f;
    }

    // 피격 모션 (적 공격 / 체력 소모 카드). 0.5초 동안 기다려 피해 처리와 박자를 맞춘다
    public IEnumerator HurtRoutine()
    {
        if (skeletonAnimation == null) yield break;

        if (SoundManager.Instance != null && SoundManager.Instance.hitSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.hitSound);
        }

        // 0번 트랙을 비운 뒤 피격 모션 재생, 끝나면 대기 모션
        skeletonAnimation.AnimationState.ClearTrack(0);
        skeletonAnimation.AnimationState.SetAnimation(0, hurtAnim, false);
        skeletonAnimation.AnimationState.AddAnimation(0, idleAnim, true, 0f);

        yield return new WaitForSeconds(0.5f);
    }

    // 회복 카드 / 휴식 때 모션
    public IEnumerator HealRoutine(float duration = 1.0f, bool loop = false)
    {
        if (skeletonAnimation == null) yield break;

        if (SoundManager.Instance != null && SoundManager.Instance.healSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.healSound);
        }

        skeletonAnimation.AnimationState.ClearTrack(0);

        // charge 모션 재생 (loop에 따라 반복)
        skeletonAnimation.AnimationState.SetAnimation(0, chargeAnim, loop);

        yield return new WaitForSeconds(duration);

        // 끝나면 대기 모션
        PlayIdle();
    }

    // 패배 (체력이 0이 됐을 때). 쓰러진 자세로 남아야 하므로 반복하지 않는다
    public void PlayDefeat()
    {
        if (skeletonAnimation == null) return;

        skeletonAnimation.AnimationState.ClearTrack(0);

        skeletonAnimation.AnimationState.SetAnimation(0, defeatAnim, false);
    }

    // 방어 카드 모션 (attack3을 방패 모션으로 사용)
    public IEnumerator ShieldRoutine(float duration = 1.0f)
    {
        if (skeletonAnimation == null) yield break;

        if (SoundManager.Instance != null && SoundManager.Instance.shieldSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.shieldSound);
        }

        // 방어 모션 재생 후 대기 모션 예약
        skeletonAnimation.AnimationState.ClearTrack(0);
        skeletonAnimation.AnimationState.SetAnimation(0, attackAnim3, false);
        skeletonAnimation.AnimationState.AddAnimation(0, idleAnim, true, 0f);

        // 방어 모션이 끝날 때까지 대기
        yield return new WaitForSeconds(duration);

        PlayIdle();
    }
}