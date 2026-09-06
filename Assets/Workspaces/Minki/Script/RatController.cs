using System.Collections;
using UnityEngine;

public class RatController : EnemyBase
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

        // 행동 중이 아닐 때 키보드 '3'번을 누르면 딜레이를 거친 뒤 행동 실행
        if (!isActing && (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)))
        {
            StartCoroutine(ActionWithDelay(2.0f)); // 2초 동안 머리 위 아이콘을 인지할 시간 부여
        }
    }

    // 지정한 시간 동안 대기했다가 몬스터의 의도 행동을 실행하는 코루틴
    private IEnumerator ActionWithDelay(float delayTime)
    {
        // 플레이어가 머리 위의 의도(칼/갑옷 아이콘)를 볼 수 있도록 잠시 대기
        yield return new WaitForSeconds(delayTime);

        // 부모 클래스에 정의된 행동 실행 함수 호출 (모션 종료 후 다음 의도로 자동 갱신됨)
        ExecuteAction();
    }

    // 쥐(Rat)만의 고유한 공격 모션: 다가가서 위아래로 빠르게 할퀴기
    protected override IEnumerator PlayCustomAttack()
    {
        // 1. 플레이어 쪽으로 빠르게 다가감 (왼쪽으로 이동)
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

        // 2. 위아래로 할퀴는 모션 (상하 흔들기 반복)
        float scratchTimer = 0;
        float scratchDuration = 0.3f;
        while (scratchTimer <= scratchDuration)
        {
            float scratchOffset = Mathf.Sin(scratchTimer * 30f) * 0.3f;
            transform.position = targetPos + new Vector3(0, scratchOffset, 0);
            scratchTimer += Time.deltaTime;
            yield return null;
        }

        // 3. 제자리(원래 위치)로 돌아오기
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

    // 쥐(Rat)만의 고유한 방어 모션: 앞으로 짖으며(왕왕) 단단해지는 느낌
    protected override IEnumerator PlayCustomDefend()
    {
        Debug.Log("쥐가 위협적으로 짖으며 방어태세를 취합니다!");

        // 1. 앞으로 살짝 튀어나오기 (왕왕 짖는 듯한 전진)
        Vector3 lungePos = originalPosition + new Vector3(-1.2f, 0, 0);
        float timer = 0;
        float lungeDuration = 0.1f;

        while (timer <= lungeDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, lungePos, timer / lungeDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // 2. 짖는 동안 몸을 바르르 떨며 단단해지는 연출 (미세한 진동과 크기 변화)
        float barkTimer = 0;
        float barkDuration = 0.35f;
        while (barkTimer <= barkDuration)
        {
            float shakeX = Random.Range(-0.08f, 0.08f);
            float shakeY = Random.Range(-0.08f, 0.08f);
            transform.position = lungePos + new Vector3(shakeX, shakeY, 0);
            
            // 단단해지듯 순간적으로 크기를 살짝 압축/확장
            float scaleBounce = baseScale + Mathf.Sin(barkTimer * 40f) * 0.1f;
            transform.localScale = new Vector3(scaleBounce, scaleBounce, scaleBounce);

            barkTimer += Time.deltaTime;
            yield return null;
        }

        // 3. 원래 위치와 크기로 복귀
        transform.position = originalPosition;
        transform.localScale = new Vector3(baseScale, baseScale, baseScale);
    }
}