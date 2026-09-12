using System.Collections;
using UnityEngine;

public class RatController : EnemyController
{
    protected override void Start()
    {
        base.Start();
        // 게임 시작 시 무작위로 공격(true) 또는 방어(false) 의도 지정
        isNextAttack = Random.value > 0.5f;
        UpdateIntentUI();
    }

    protected override void Update()
    {
        // 부모 클래스의 대기 중 꿀렁임(숨쉬기) 연출 유지
        base.Update();

        // 행동 중이 아닐 때 키보드 '3'번을 누르면 딜레이를 거친 뒤 행동 실행 (테스트용)
        if (!isActing && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)))
        {
            StartCoroutine(ActionWithDelay(2.0f)); 
        }
    }

    // 지정한 시간 동안 대기했다가 몬스터의 의도 행동을 실행하는 코루틴
    private IEnumerator ActionWithDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        ExecuteAction();
    }

    // 쥐(Rat)만의 고유한 공격 모션
    protected override IEnumerator PlayCustomAttack()
    {
        Vector3 targetPos = originalPosition + new Vector3(-4f, 0, 0);
        float timer = 0;
        float moveDuration = 0.15f;

        while (timer <= moveDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, targetPos, timer / moveDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        float scratchTimer = 0;
        float scratchDuration = 0.3f;
        while (scratchTimer <= scratchDuration)
        {
            float scratchOffset = Mathf.Sin(scratchTimer * 30f) * 0.3f;
            transform.position = targetPos + new Vector3(0, scratchOffset, 0);
            scratchTimer += Time.deltaTime;
            yield return null;
        }

        timer = 0;
        float returnDuration = 0.3f;
        while (timer <= returnDuration)
        {
            transform.position = Vector3.Lerp(targetPos, originalPosition, timer / returnDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }

    // 쥐(Rat)만의 고유한 방어 모션
    protected override IEnumerator PlayCustomDefend()
    {
        Debug.Log("쥐가 위협적으로 짖으며 방어태세를 취합니다!");

        Vector3 lungePos = originalPosition + new Vector3(-1.2f, 0, 0);
        float timer = 0;
        float lungeDuration = 0.1f;

        while (timer <= lungeDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, lungePos, timer / lungeDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        float barkTimer = 0;
        float barkDuration = 0.35f;
        while (barkTimer <= barkDuration)
        {
            float shakeX = Random.Range(-0.08f, 0.08f);
            float shakeY = Random.Range(-0.08f, 0.08f);
            transform.position = lungePos + new Vector3(shakeX, shakeY, 0);
            
            float scaleBounce = baseScale + Mathf.Sin(barkTimer * 40f) * 0.1f;
            transform.localScale = new Vector3(scaleBounce, scaleBounce, scaleBounce);

            barkTimer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        transform.localScale = new Vector3(baseScale, baseScale, baseScale);
    }
}