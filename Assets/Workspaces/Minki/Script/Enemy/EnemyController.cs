using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 모든 적(일반 몬스터, 중간보스, 보스)의 공통 동작.
// - 다음 행동(공격 / 방어)을 정하고 머리 위 의도 아이콘과 숫자를 표시
// - 적 턴에 TurnManager가 ExecuteAction을 부르면 공격 / 방어 연출 후 실제 피해 / 방어도 적용
// - 맞았을 때 반짝임 + 피격음, 세이브 / 로드 시 다음 행동 저장 / 복원
// 몬스터마다 다른 공격 / 방어 모션은 자식 클래스가 PlayCustomAttack / PlayCustomDefend로 구현한다.
public abstract class EnemyController : MonoBehaviour
{
    public UnitBase unitData; // 스탯을 가진 유닛 (자식 오브젝트에서 찾음)

    [Header("의도(Intent) UI")]
    public Image intentIcon; 
    public Sprite attackSprite; 
    public Sprite defendSprite;
    protected bool isNextAttack = true; // 다음 행동 (true: 공격, false: 방어)
    public int currentAttack; // 다음 공격의 피해 (아이콘 숫자)
    public int currentDefend; // 다음 방어의 방어도 (아이콘 숫자)
    public TextMeshProUGUI actionText; // 의도 아이콘 옆 숫자
    protected AttackData selectedAttack; // 이번에 뽑힌 공격 패턴

    [Header("공통 연출 설정")]
    public float baseScale = 2f; // 숨쉬기 연출의 기준 크기
    protected bool isActing = false; // 공격 / 방어 연출 중인지

    [Header("월드 스페이스 UI 추적 설정")]
    public Canvas intentCanvas; // 머리 위 의도 아이콘 캔버스 (월드 스페이스)

    [Header("피격 이펙트 (공통)")]
    public Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f); // 맞았을 때 잠깐 바뀌는 색
    public float hitFlashDuration = 0.1f; // 색이 유지되는 시간
    public GameObject hitEffectPrefab; // 맞을 때 생성할 이펙트 프리팹 (비우면 색 반짝임만)
    public Vector3 hitEffectOffset = Vector3.zero;

    [Header("효과음")]
    [Tooltip("이 적만의 공격 효과음 (비워두면 SoundManager의 공통 enemyAttackSound)")]
    public AudioClip attackSound;
    [Tooltip("공격 효과음 재생 횟수 (예: 화살 2연사)")]
    public int attackSoundRepeat = 1;
    [Tooltip("반복 재생 사이 간격 (초)")]
    public float attackSoundInterval = 0.15f;

    [Header("몸 표시 보정 (전투 배경에 가려지지 않도록)")]
    [Tooltip("몸 조각들의 정렬 순서를 최소 이 값 이상으로 올림 (전투 배경 BackgroundFloor가 2). 0 이하면 사용 안 함")]
    public int minBodySortingOrder = 10;

    // 실제 체력을 관리하는 유닛 (BattleFactory가 스폰한 프리팹에 붙어 있음)
    protected EnemyUnit hitUnit;
    protected bool hitIsDead = false;
    protected int hitLastHp; // 바로 전 체력 (피해를 입었는지 비교용)

    private SpriteRenderer hitRenderer;
    private Coroutine hitFlashCoroutine;

    protected virtual void Start()
    {
        actionText = GetComponentInChildren<TextMeshProUGUI>();
        unitData = GetComponentInChildren<UnitBase>();

        // 첫 행동을 무작위로 정하고 아이콘 표시 (불러온 전투면 저장된 행동으로 복원)
        if (!RestoreSavedIntent())
            DecideNextAction();

        // 의도 아이콘에 마우스를 올리면 설명창 (내용은 GetIntentTooltip)
        if (intentIcon != null)
        {
            IntentTooltipHover hover = intentIcon.GetComponent<IntentTooltipHover>();
            if (hover == null) hover = intentIcon.gameObject.AddComponent<IntentTooltipHover>();
            hover.Init(this);
        }

        // ----- 피격 연출 준비 -----
        hitRenderer = GetComponent<SpriteRenderer>();
        if (hitRenderer == null) hitRenderer = GetComponentInChildren<SpriteRenderer>();

        hitUnit = GetComponent<EnemyUnit>();
        hitUnit.attackPower = unitData.statData.attacks[0].damage;

        if (hitUnit != null)
        {
            hitLastHp = hitUnit.statData != null ? hitUnit.statData.currentHp : 0;
            hitUnit.OnHpChanged += HandleHpChanged;
        }
    }

    protected virtual void Update()
    {
        // 행동 중이 아닐 때 숨쉬듯 꿀렁이는 연출 (애니메이션이 있는 자식 클래스는 override해서 끔)
        if (!isActing)
        {
            float bounce = Mathf.Sin(Time.time * 2f) * 0.05f;
            transform.localScale = new Vector3(baseScale + bounce, baseScale - bounce, baseScale);
        }

        // 의도 아이콘 캔버스가 머리 위를 따라다님
        if (intentCanvas != null)
        {
            intentCanvas.transform.position = transform.position + new Vector3(0, 2.0f, 0);
        }
    }

    private void OnDestroy()
    {
        if (hitUnit != null) hitUnit.OnHpChanged -= HandleHpChanged;
    }

    // 실제 체력(EnemyUnit)이 바뀔 때마다 호출. 자식 클래스(보스 등)는 override해서 피격 / 사망 애니메이션을 덧붙인다
    // (공통 반짝임을 위해 base.HandleHpChanged를 먼저 호출할 것)
    protected virtual void HandleHpChanged(int current, int max)
    {
        if (hitIsDead) return;

        bool tookDamage = current < hitLastHp;

        if (tookDamage)
        {
            PlayHitEffect();
            PlayEnemySound(SoundManager.Instance != null ? SoundManager.Instance.enemyHitSound : null);
        }

        if (current <= 0)
        {
            hitIsDead = true;
        }

        hitLastHp = current;
    }

    // 공격 모션에서 플레이어를 때리는 순간에 호출 → 화면 흔들림 (HitShake). incomingDamage = 이번 공격력
    protected void NotifyAttackImpact(int incomingDamage)
    {
        HitShake.OnEnemyAttackImpact(incomingDamage);
    }

    // 공격 효과음 (이 적만의 소리가 없으면 공통 소리). 연사 공격은 간격을 두고 여러 번
    protected void PlayAttackSound()
    {
        AudioClip clip = attackSound != null ? attackSound : (SoundManager.Instance != null ? SoundManager.Instance.enemyAttackSound : null);
        PlayEnemySound(clip);
        if (attackSoundRepeat > 1 && clip != null)
            StartCoroutine(RepeatAttackSound(clip));
    }

    private IEnumerator RepeatAttackSound(AudioClip clip)
    {
        for (int i = 1; i < attackSoundRepeat; i++)
        {
            yield return new WaitForSeconds(attackSoundInterval);
            PlayEnemySound(clip);
        }
    }

    // 방어 효과음 (적 전용 소리가 없으면 공통 방어도 소리)
    protected void PlayDefendSound()
    {
        if (SoundManager.Instance == null) return;
        AudioClip clip = SoundManager.Instance.enemyDefendSound;
        PlayEnemySound(clip != null ? clip : SoundManager.Instance.shieldSound);
    }

    protected void PlayEnemySound(AudioClip clip)
    {
        if (SoundManager.Instance != null && clip != null)
            SoundManager.Instance.PlaySFX(clip);
    }

    // 맞는 순간의 공통 연출: 스프라이트 색 반짝임 + (있으면) 이펙트 프리팹 생성
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
        // 맞기 직전 색을 저장했다가 복원 (알파가 계속 바뀌는 연출과 충돌하지 않도록)
        Color before = hitRenderer.color;
        hitRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        hitRenderer.color = before;
    }

    // 에셋 프리팹을 자식으로 넣으면 생기는 문제를 자동으로 보정한다
    // 1) 자식의 Z 위치가 -75 같은 값이면 카메라(Z -10) 뒤로 가서 안 보임 → Z를 0으로
    // 2) 몸 조각 정렬 순서가 0~2면 전투 배경(BackgroundFloor 2) 뒤에 가려짐 → 서로의 순서는 유지한 채 위로 올림
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

    // 적 턴에 TurnManager가 호출: 정해 둔 행동(공격 / 방어) 실행
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

    // 공격: 모션 → 플레이어 피격 모션 → 다음 행동 결정 → 실제 피해
    IEnumerator AttackRoutineWrapper()
    {
        isActing = true;
        // 아이콘에 보였던 이번 턴 피해를 고정 (DecideNextAction이 다음 턴 값으로 덮어쓰기 전에)
        int attackThisTurn = currentAttack;
        yield return StartCoroutine(PlayCustomAttack());

        // 플레이어 피격 모션이 끝날 때까지 기다린 뒤 피해 적용
        if (PlayerSpineController.Instance != null)
        {
            yield return StartCoroutine(PlayerSpineController.Instance.HurtRoutine());
        }

        DecideNextAction();

        // 공격 모션의 타격 순간에 이미 흔들었으면 여기서는 다시 흔들지 않음 (HitShake)
        using (HitShake.PlayerDamageScope())
            GameManager.Instance.currentPlayer.TakeDamage(attackThisTurn);

        isActing = false;
    }

    // 방어: 모션 → 다음 행동 결정 → 방어도 획득
    IEnumerator DefendRoutineWrapper()
    {
        isActing = true;
        // 아이콘에 보였던 이번 턴 방어도를 고정
        int defendThisTurn = currentDefend;
        yield return StartCoroutine(PlayCustomDefend());

        DecideNextAction();

        hitUnit.AddShield(defendThisTurn);

        isActing = false;
    }

    // 다음 행동을 무작위로 정하고 아이콘 / 숫자 갱신 (공격이면 공격 패턴 중 하나를 무작위로)
    protected void DecideNextAction()
    {
        isNextAttack = (Random.value > 0.5f);
        Debug.Log($"[디버깅] Decide 진입! 다음 행동은 공격인가? {isNextAttack}"); 

        if (unitData != null && unitData.statData != null)
        {
            if (isNextAttack)
            {
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
                currentDefend = unitData.statData.defend;

                if (actionText != null)
                {
                    actionText.text = currentDefend.ToString();
                    Debug.Log($"[디버깅] 방어력 텍스트 갱신: {currentDefend}"); 
                }
            }
        }

        UpdateIntentUI();
    }

    // ===== 저장 / 불러오기 =====

    // 불러온 전투의 몬스터 상태 (GameManager가 스폰 직후, 이 몬스터의 Start보다 먼저 넣어 줌)
    protected MonsterSaveData savedState;

    public void SetSavedState(MonsterSaveData state)
    {
        savedState = state;
    }

    protected bool HasSavedIntent => savedState != null && savedState.hasIntent && !string.IsNullOrEmpty(savedState.intent);

    // 세이브할 때 다음 행동 기록 (보스는 override해서 기믹 상태까지)
    public virtual void WriteSaveState(MonsterSaveData data)
    {
        data.hasIntent = true;
        data.intent = isNextAttack ? "Attack" : "Defend";
        data.intentValue = isNextAttack ? currentAttack : currentDefend;
    }

    // 저장된 행동이 있으면 그대로 복원하고 true (없으면 false → 무작위로 뽑음)
    protected virtual bool RestoreSavedIntent()
    {
        if (!HasSavedIntent) return false;

        isNextAttack = savedState.intent != "Defend";

        if (isNextAttack)
        {
            currentAttack = savedState.intentValue;
            if (actionText != null) actionText.text = currentAttack.ToString();
        }
        else
        {
            currentDefend = savedState.intentValue;
            if (actionText != null) actionText.text = currentDefend.ToString();
        }

        UpdateIntentUI();
        return true;
    }

    // 의도 아이콘 갱신. 아이콘은 IntentIconSet에서 몬스터 id와 수치로 정한다 (없으면 프리팹의 Attack / Defend Sprite)
    protected void UpdateIntentUI()
    {
        if (intentIcon == null) return;

        if (isNextAttack)
        {
            intentIcon.sprite = GetIntentSprite(true, currentAttack);
        }
        else
        {
            intentIcon.sprite = GetIntentSprite(false, currentDefend);
        }
    }

    // 의도 아이콘 설명창 색 (TextMeshPro 색 코드)
    public const string TooltipAttackColor = "#FF6B6B";
    public const string TooltipDefendColor = "#6FA8FF";
    public const string TooltipChargeColor = "#FFD24D";
    public const string TooltipNoteColor = "#BBBBBB";

    // 의도 아이콘 설명 (보스는 override해서 기믹까지 설명)
    public virtual string GetIntentTooltip()
    {
        if (isNextAttack)
            return $"<color={TooltipAttackColor}><size=32>공격</size></color>\n피해 {currentAttack}";

        return $"<color={TooltipDefendColor}><size=32>방어</size></color>\n방어도 +{currentDefend}";
    }

    protected Sprite GetIntentSprite(bool isAttack, int value)
    {
        return IntentIconSet.Resolve(MonsterId, isAttack, value, isAttack ? attackSprite : defendSprite);
    }

    // 서버 Monster.json id (BattleFactory가 스탯을 넣을 때 기록, 없으면 -1)
    protected int MonsterId
    {
        get
        {
            UnitBase u = hitUnit != null ? hitUnit : unitData;
            return u != null && u.statData != null ? u.statData.monsterId : -1;
        }
    }

    // 몬스터마다 모션이 다르므로 자식 클래스에서 구현
    protected abstract IEnumerator PlayCustomAttack();
    protected abstract IEnumerator PlayCustomDefend();
}