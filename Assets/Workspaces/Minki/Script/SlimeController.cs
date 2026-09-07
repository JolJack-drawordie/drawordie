using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlimeController : EnemyBase
{
    [Header("슬라임 전용 UI")]
    public Slider hpSlider;
    public TMPro.TextMeshProUGUI hpText;

    protected override void Start()
    {
        base.Start(); // EnemyBase의 Start 실행 (체력 초기화 및 첫 의도 무작위 지정)
        UpdateHpUI();
    }

    protected override void Update()
    {
        base.Update(); // EnemyBase의 Update 실행 (기본 꿀렁임 및 월드 캔버스 위치 추적)

        // 테스트용 (필요시 사용 후 나중에 제거 가능)
        if (Input.GetKeyDown(KeyCode.Alpha3) && !isActing)
        {
            ExecuteAction();
        }
    }

    // 데미지를 입을 때 체력바 UI도 함께 갱신되도록 오버라이드
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        UpdateHpUI();
    }

    // 체력 UI 업데이트 헬퍼 함수
    void UpdateHpUI()
    {
        if (hpSlider != null)
        {
            hpSlider.value = (float)currentHp / maxHp;
        }
        if (hpText != null)
        {
            hpText.text = $"{currentHp} / {maxHp}";
        }
    }

    // 슬라임의 커스텀 공격: 왼쪽으로 슈슉 돌진했다 돌아오기
    protected override IEnumerator PlayCustomAttack()
    {
        Debug.Log("슬라임의 돌진 공격!");
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
    }

    // 슬라임의 커스텀 방어: 제자리에서 콩 점프하기
    protected override IEnumerator PlayCustomDefend()
    {
        Debug.Log("슬라임이 방어력을 얻었습니다! (제자리 점프)");

        float timer = 0;
        while (timer <= 0.3f)
        {
            float jump = Mathf.Sin((timer / 0.3f) * Mathf.PI) * 0.5f;
            transform.position = originalPosition + new Vector3(0, jump, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }
}