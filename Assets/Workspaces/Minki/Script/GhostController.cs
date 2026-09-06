using System.Collections;
using UnityEngine;

public class GhostController : EnemyBase
{
    private SpriteRenderer spriteRenderer;

    protected override void Start()
    {
        base.Start();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 게임 시작 시 무작위로 공격(true) 또는 방어(false) 의도 지정
        isNextAttack = Random.value > 0.5f;
        UpdateIntentUI();

        // 평소에 계속 투명해졌다 나타나는 숨쉬기/부유 연출 코루틴 시작
        StartCoroutine(IdleFadeRoutine());
    }

    protected override void Update()
    {
        // 부모 클래스의 기본 꿀렁임(숨쉬기) 연출 유지
        base.Update();

        // 쥐와 똑같이 '3'번 키를 누르면 딜레이를 거친 뒤 행동 실행
        if (!isActing && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)))
        {
            StartCoroutine(ActionWithDelay(2.0f)); // 2초 동안 머리 위 아이콘을 인지할 시간 부여
        }
    }

    // 지정한 시간 동안 대기했다가 고스트의 의도 행동을 실행하는 코루틴
    private IEnumerator ActionWithDelay(float delayTime)
    {
        // 플레이어가 머리 위의 의도(칼/갑옷 아이콘)를 볼 수 있도록 잠시 대기
        yield return new WaitForSeconds(delayTime);

        // 부모 클래스에 정의된 행동 실행 함수 호출 (모션 종료 후 다음 의도로 자동 갱신됨)
        ExecuteAction();
    }

    // 평소에 유령처럼 투명해졌다 선명해졌다를 반복하는 아이들 연출
    private IEnumerator IdleFadeRoutine()
    {
        while (true)
        {
            // 행동 중이 아닐 때만 은은하게 깜빡임
            if (!isActing)
            {
                // 서서히 투명해지기 (알파 0.4까지)
                yield return StartCoroutine(FadeAlpha(spriteRenderer.color.a, 0.4f, 1.0f));
                // 서서히 선명해지기 (알파 1.0까지)
                yield return StartCoroutine(FadeAlpha(spriteRenderer.color.a, 1.0f, 1.0f));
            }
            yield return null;
        }
    }

    // 투명도(Alpha)를 부드럽게 조절하는 공통 함수
    private IEnumerator FadeAlpha(float startAlpha, float endAlpha, float duration)
    {
        Color color = spriteRenderer.color;
        float timer = 0f;

        while (timer < duration)
        {
            // 행동 중에는 아이들 페이드를 잠시 멈추고 고유 모션의 알파 제어에 양보
            if (isActing) yield break;

            timer += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            spriteRenderer.color = new Color(color.r, color.g, color.b, currentAlpha);
            yield return null;
        }
        spriteRenderer.color = new Color(color.r, color.g, color.b, endAlpha);
    }

    // 고스트만의 고유 공격 모션: 완전히 투명해져서 파고들었다가 덮치기
    protected override IEnumerator PlayCustomAttack()
    {
        // 1. 완전히 투명해지면서 시야에서 사라짐
        yield return StartCoroutine(ChangeAlphaInstant(0f, 0.2f));

        // 2. 투명한 상태로 플레이어 쪽(왼쪽)으로 스윽 이동
        Vector3 targetPos = originalPosition + new Vector3(-3.5f, 0, 0);
        float timer = 0;
        float moveDuration = 0.3f;

        while (timer <= moveDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, targetPos, timer / moveDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        // 3. 플레이어 코앞에서 다시 선명하게 나타나며(페이드 인) 일격 찌르기
        yield return StartCoroutine(ChangeAlphaInstant(1f, 0.2f));

        // 살짝 덮치는 타격 진동
        float strikeTimer = 0;
        while (strikeTimer <= 0.15f)
        {
            transform.position = targetPos + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
            strikeTimer += Time.deltaTime;
            yield return null;
        }

        // 4. 원래 위치로 돌아오기
        timer = 0;
        while (timer <= 0.3f)
        {
            transform.position = Vector3.Lerp(targetPos, originalPosition, timer / 0.3f);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }

    // 고스트만의 고유 방어 모션: 영체 장막을 두르며 투명한 방어 태세 취하기
    protected override IEnumerator PlayCustomDefend()
    {
        // 1. 제자리에서 위로 살짝 떠오르며 푸른빛/영체 형태로 변환 (반투명화)
        Vector3 floatPos = originalPosition + new Vector3(0, 0.8f, 0);
        float timer = 0;
        
        while (timer <= 0.2f)
        {
            transform.position = Vector3.Lerp(originalPosition, floatPos, timer / 0.2f);
            timer += Time.deltaTime;
            yield return null;
        }

        // 몸이 유령 장막처럼 옅어짐 (알파 0.3)
        yield return StartCoroutine(ChangeAlphaInstant(0.3f, 0.2f));

        // 2. 장막을 유지하며 영롱하게 떨리는 연출
        float defendTimer = 0;
        while (defendTimer <= 0.4f)
        {
            float floatOffset = Mathf.Sin(defendTimer * 20f) * 0.1f;
            transform.position = floatPos + new Vector3(0, floatOffset, 0);
            
            // 크기도 살짝 커졌다가 돌아오는 부유감
            float scaleBounce = baseScale + Mathf.Sin(defendTimer * 15f) * 0.05f;
            transform.localScale = new Vector3(scaleBounce, scaleBounce, scaleBounce);

            defendTimer += Time.deltaTime;
            yield return null;
        }

        // 3. 원래 위치, 투명도, 크기로 복귀
        yield return StartCoroutine(ChangeAlphaInstant(1f, 0.2f));

        timer = 0;
        while (timer <= 0.2f)
        {
            transform.position = Vector3.Lerp(floatPos, originalPosition, timer / 0.2f);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        transform.localScale = new Vector3(baseScale, baseScale, baseScale);
    }

    // 모션 중에 즉각적으로 알파값을 목표치까지 바꾸는 내부 헬퍼 코루틴
    private IEnumerator ChangeAlphaInstant(float targetAlpha, float duration)
    {
        Color color = spriteRenderer.color;
        float startAlpha = color.a;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            spriteRenderer.color = new Color(color.r, color.g, color.b, currentAlpha);
            yield return null;
        }
        spriteRenderer.color = new Color(color.r, color.g, color.b, targetAlpha);
    }
}