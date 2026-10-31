using System;
using UnityEngine;

// 피해량에 비례해 화면을 흔든다 (타격감). 세기 조절: Resources/HitShakeSettings
//
// [내가 적을 때릴 때] 피해는 플레이어 공격 루틴(달려감 → 공격 모션 → 복귀)이 끝난 뒤에 들어가지만,
//   흔들림은 공격 모션의 타격 순간(PlayerSpineController.OnAttackImpact)에 미리 재생한다.
//   EnemyTarget: ArmAttack(적, 피해) → 공격 루틴 → using (AttackDamageScope()) TakeDamage
//
// [적이 나를 때릴 때] 적 공격 모션의 타격 순간(EnemyController.NotifyAttackImpact)에 미리 재생한다.
//   EnemyController / BossController2: 타격 순간 NotifyAttackImpact(공격력) → 모션 후 using (PlayerDamageScope()) 피해
//
// 타격 순간에 이미 흔들었으면 실제 피해가 들어갈 때는 다시 흔들지 않음.
// 그 밖의 피해(가시, 독, 타격 순간 알림이 없는 몬스터 등)는 피해가 들어가는 순간 흔듦 (UnitBase.OnCombatNumber).
// 방어도에 전부 막힌 공격은 흔들지 않음.
public static class HitShake
{
    // 타격 순간 흔들림 후 이 시간 안에 실제 피해가 안 들어오면 (전투 종료 등) 표시를 버림
    private const float PreShakeValidSeconds = 6f;

    // 내 공격
    private static EnemyUnit armedTarget;
    private static int armedDamage;
    private static bool armedShook;
    private static int enemySuppressDepth;

    // 적 공격
    private static float playerPreShakeTime = -1f;
    private static int playerSuppressDepth;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        armedTarget = null;
        armedDamage = 0;
        armedShook = false;
        enemySuppressDepth = 0;
        playerPreShakeTime = -1f;
        playerSuppressDepth = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // 도메인 리로드 꺼진 상태에서 중복 구독 방지
        UnitBase.OnCombatNumber -= HandleCombatNumber;
        UnitBase.OnCombatNumber += HandleCombatNumber;
        PlayerSpineController.OnAttackImpact -= HandlePlayerAttackImpact;
        PlayerSpineController.OnAttackImpact += HandlePlayerAttackImpact;
    }

    // ===================== 내가 적을 때릴 때 =====================

    // 공격 모션 시작 전에 호출: 다음 타격 순간에 이 적/피해량 기준으로 흔듦
    public static void ArmAttack(EnemyUnit target, int incomingDamage)
    {
        armedTarget = target;
        armedDamage = incomingDamage;
        armedShook = false;
    }

    // 공격 모션 뒤 실제 피해를 줄 때 감싸서 사용
    public static DamageScope AttackDamageScope()
    {
        bool skip = armedShook;
        armedTarget = null;
        armedDamage = 0;
        armedShook = false;
        if (skip) enemySuppressDepth++;
        return new DamageScope(skip, false);
    }

    private static void HandlePlayerAttackImpact()
    {
        if (armedTarget == null || armedShook || armedTarget.statData == null) return;

        // 실제 피해 계산(EnemyUnit/UnitBase.TakeDamage)과 같은 방식으로 체력에 들어갈 피해를 미리 계산
        int incoming = armedDamage;
        if (GameFlowData.debugBigDamage && incoming > 0)
            incoming = Mathf.Max(incoming, GameFlowData.DebugBigDamageAmount);

        armedShook = true;
        Shake(HpDamageAfterShield(armedTarget, incoming), true);
    }

    // ===================== 적이 나를 때릴 때 =====================

    // 적 공격 모션의 타격 순간에 호출 (EnemyController.NotifyAttackImpact)
    public static void OnEnemyAttackImpact(int incomingDamage)
    {
        PlayerUnit player = GameManager.Instance != null ? GameManager.Instance.currentPlayer : null;
        if (player == null || player.statData == null) return;

        playerPreShakeTime = Time.unscaledTime;
        Shake(HpDamageAfterShield(player, incomingDamage), false);
    }

    // 적 공격 모션 뒤 실제 피해를 줄 때 감싸서 사용
    public static DamageScope PlayerDamageScope()
    {
        bool skip = playerPreShakeTime >= 0f && Time.unscaledTime - playerPreShakeTime <= PreShakeValidSeconds;
        playerPreShakeTime = -1f;
        if (skip) playerSuppressDepth++;
        return new DamageScope(false, skip);
    }

    // ===================== 공통 =====================

    public struct DamageScope : IDisposable
    {
        private readonly bool enemySuppressed;
        private readonly bool playerSuppressed;

        public DamageScope(bool enemySuppressed, bool playerSuppressed)
        {
            this.enemySuppressed = enemySuppressed;
            this.playerSuppressed = playerSuppressed;
        }

        public void Dispose()
        {
            if (enemySuppressed && enemySuppressDepth > 0) enemySuppressDepth--;
            if (playerSuppressed && playerSuppressDepth > 0) playerSuppressDepth--;
        }
    }

    private static int HpDamageAfterShield(UnitBase unit, int incoming)
    {
        return Mathf.Max(0, incoming - unit.statData.currentShield);
    }

    private static void HandleCombatNumber(UnitBase unit, UnitBase.NumberKind kind, int amount)
    {
        if (kind != UnitBase.NumberKind.Damage || amount <= 0) return;

        if (unit is EnemyUnit)
        {
            if (enemySuppressDepth > 0) return; // 내 공격 모션 타격 순간에 이미 흔듦
            Shake(amount, true);
        }
        else if (unit is PlayerUnit)
        {
            if (playerSuppressDepth > 0) return; // 적 공격 모션 타격 순간에 이미 흔듦
            Shake(amount, false);
        }
    }

    private static void Shake(int hpDamage, bool enemyWasHit)
    {
        if (hpDamage <= 0) return;

        HitShakeSettings settings = HitShakeSettings.Load();
        if (enemyWasHit ? !settings.shakeOnEnemyHit : !settings.shakeOnPlayerHit) return;

        float amplitude, duration;
        if (enemyWasHit) settings.EvaluateEnemyHit(hpDamage, out amplitude, out duration);
        else settings.EvaluatePlayerHit(hpDamage, out amplitude, out duration);
        CameraShaker.Shake(amplitude, duration);
    }
}
