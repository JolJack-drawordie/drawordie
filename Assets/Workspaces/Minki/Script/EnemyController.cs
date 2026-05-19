using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    [Header("UI 연결")]
    public Slider hpSlider;
    public TMPro.TextMeshProUGUI hpText;
    public Image intentIcon; // Hierarchy의 IntentIcon 연결

    [Header("아이콘 이미지")]
    public Sprite attackSprite; // 검 이미지 연결
    public Sprite defendSprite; // 갑옷/방패 이미지 연결

    [Header("설정")]
    public float baseScale = 2f;
    private Vector3 originalPosition;
    private bool isAttacking = false;
    
    // 핵심: 다음 행동이 무엇인지 기억하는 변수
    private bool isNextAttack = true; 

    void Start()
    {
        originalPosition = transform.position;
        UpdateIntentUI(); // 시작할 때 첫 번째 의도(검) 표시
    }

    void Update()
    {
        // 평소 숨쉬는 듯한 꿀렁임
        if (!isAttacking)
        {
            float bounce = Mathf.Sin(Time.time * 2f) * 0.05f;
            transform.localScale = new Vector3(baseScale + bounce, baseScale - bounce, baseScale);
        }

        // 스페이스바 누르면 현재 '예고된' 행동 실행
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            if (!isAttacking) ExecuteAction();
        }
    }

    // 순서대로 행동을 실행하는 함수
    void ExecuteAction()
    {
        if (isNextAttack)
        {
            // 공격 예고 상태였다면 돌진 공격!
            StartCoroutine(AttackRoutine());
        }
        else
        {
            // 방어 예고 상태였다면 제자리 점프!
            StartCoroutine(DefendRoutine());
        }

        // 중요: 행동이 시작됐으니 다음 행동은 반대로 바꿈
        isNextAttack = !isNextAttack;
        
        // 아이콘도 다음 행동에 맞춰 미리 변경
        UpdateIntentUI();
    }

    void UpdateIntentUI()
    {
        if (intentIcon == null) return;

        // 로그를 찍어서 현재 어떤 아이콘으로 바뀌어야 하는지 콘솔창에 표시합니다.
        if (isNextAttack) 
        {
            intentIcon.sprite = attackSprite;
            Debug.Log("다음 행동 예고: 공격 (칼 아이콘)");
        }
        else 
        {
            intentIcon.sprite = defendSprite;
            Debug.Log("다음 행동 예고: 방어 (방패 아이콘)");
        }
    }

    // 공격: 왼쪽으로 슈슉 돌진했다 돌아오기
    IEnumerator AttackRoutine()
    {
        isAttacking = true;
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
        isAttacking = false;
    }

    // 방어: 제자리에서 콩 점프하기
    IEnumerator DefendRoutine()
    {
        isAttacking = true;
        Debug.Log("방어력을 얻었습니다!");

        float timer = 0;
        while (timer <= 0.3f)
        {
            float jump = Mathf.Sin((timer / 0.3f) * Mathf.PI) * 0.5f;
            transform.position = originalPosition + new Vector3(0, jump, 0);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        isAttacking = false;
    }
}