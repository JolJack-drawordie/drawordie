using System.Collections;
using UnityEngine;

public class SlimeController : EnemyController
{
    // 슬라임의 공격 모션: 기존의 -5f 돌진 거리와 타이밍(0.15초, 0.4초) 완벽 유지
    protected override IEnumerator PlayCustomAttack()
    {
        isActing = true;
        Vector3 targetPos = originalPosition + new Vector3(-5f, 0, 0); 
        
        float timer = 0;
        while (timer <= 0.15f)
        {
            transform.position = Vector3.Lerp(originalPosition, targetPos, timer / 0.15f);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        timer = 0;
        while (timer <= 0.4f)
        {
            transform.position = Vector3.Lerp(targetPos, originalPosition, timer / 0.4f);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        isActing = false;
    }

    // 슬라임의 방어 모션: 기존의 제자리 콩 점프 높이와 타이밍(0.3초) 완벽 유지
    protected override IEnumerator PlayCustomDefend()
    {
        isActing = true;
        Debug.Log("슬라임이 방어력을 얻었습니다!");

        float timer = 0;
        while (timer <= 0.3f)
        {
            float jump = Mathf.Sin((timer / 0.3f) * Mathf.PI) * 0.5f;
            transform.position = originalPosition + new Vector3(0, jump, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        isActing = false;
    }
}