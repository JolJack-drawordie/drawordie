using System.Collections;
using UnityEngine;

public class GhostController : EnemyController
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

        // 테스트용 키 입력 (필요에 따라 유지 또는 삭제)
        if (!isActing && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)))
        {
            StartCoroutine(ActionWithDelay(2.0f)); 
        }
    }

    // 지정한 시간 동안 대기했다가 고스트의 의도 행동을 실행하는 코루틴
    private IEnumerator ActionWithDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        ExecuteAction();
    }

    // 평소에 유령처럼 투명해졌다 선명해졌다를 반복하는 아이들 연출
    private IEnumerator IdleFadeRoutine()
    {
        while (true)
        {
            if (!isActing)
            {
                yield return StartCoroutine(FadeAlpha(spriteRenderer.color.a, 0.4f, 1.0f));
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
            if (isActing) yield break;

            timer += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            spriteRenderer.color = new Color(color.r, color.g, color.b, currentAlpha);
            yield return null;
        }
        spriteRenderer.color = new Color(color.r, color.g, color.b, endAlpha);
    }

    // 고스트만의 고유 공격 모션
    protected override IEnumerator PlayCustomAttack()
    {
        yield return StartCoroutine(ChangeAlphaInstant(0f, 0.2f));

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

        yield return StartCoroutine(ChangeAlphaInstant(1f, 0.2f));

        float strikeTimer = 0;
        while (strikeTimer <= 0.15f)
        {
            transform.position = targetPos + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
            strikeTimer += Time.deltaTime;
            yield return null;
        }

        timer = 0;
        while (timer <= 0.3f)
        {
            transform.position = Vector3.Lerp(targetPos, originalPosition, timer / 0.3f);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }

    // 고스트만의 고유 방어 모션
    protected override IEnumerator PlayCustomDefend()
    {
        Vector3 floatPos = originalPosition + new Vector3(0, 0.8f, 0);
        float timer = 0;
        
        while (timer <= 0.2f)
        {
            transform.position = Vector3.Lerp(originalPosition, floatPos, timer / 0.2f);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(ChangeAlphaInstant(0.3f, 0.2f));

        float defendTimer = 0;
        while (defendTimer <= 0.4f)
        {
            float floatOffset = Mathf.Sin(defendTimer * 20f) * 0.1f;
            transform.position = floatPos + new Vector3(0, floatOffset, 0);
            
            float scaleBounce = baseScale + Mathf.Sin(defendTimer * 15f) * 0.05f;
            transform.localScale = new Vector3(scaleBounce, scaleBounce, scaleBounce);

            defendTimer += Time.deltaTime;
            yield return null;
        }

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