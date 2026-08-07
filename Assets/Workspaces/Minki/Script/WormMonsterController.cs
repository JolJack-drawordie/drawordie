using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WormMonsterController : MonoBehaviour
{
    [Header("UI 연결")]
    public Slider hpSlider;
    public TMPro.TextMeshProUGUI hpText;
    public Image intentIcon; // Hierarchy의 IntentIcon 연결

    [Header("아이콘 이미지")]
    public Sprite attackSprite; // 검 이미지 연결
    public Sprite defendSprite; // 갑옷/방패 이미지 연결

    [Header("설정")]
    public float baseScale = 3f; // 기본 크기 유지
    public bool useKeyboardInput = true; // 스페이스바 테스트용
    private Vector3 originalPosition;
    private bool isActing = false;

    // 핵심: 다음 행동이 무엇인지 기억하는 변수 (공수 순서)
    private bool isNextAttack = true;

    void Start()
    {
        originalPosition = transform.position;
        transform.localScale = new Vector3(baseScale, baseScale, baseScale);
        UpdateIntentUI(); // 시작할 때 첫 번째 의도(검) 표시
    }

    void Update()
    {
        // 평소 숨쉬는 듯한 꿀렁임 (기본 크기 기준)
        if (!isActing)
        {
            float bounce = Mathf.Sin(Time.time * 3f) * 0.05f;
            transform.localScale = new Vector3(baseScale + bounce, baseScale - bounce, baseScale);
        }

        // 스페이스바 누르면 현재 '예고된' 행동 실행
        if (useKeyboardInput && Input.GetKeyDown(KeyCode.Space))
        {
            if (!isActing) ExecuteAction();
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
            // 방어 예고 상태였다면 몸집이 커졌다가 작아지기!
            StartCoroutine(DefendRoutine());
        }

        // 행동이 시작됐으니 다음 행동은 반대로 변경
        isNextAttack = !isNextAttack;
        
        // 아이콘도 다음 행동에 맞춰 미리 변경
        UpdateIntentUI();
    }

    void UpdateIntentUI()
    {
        if (intentIcon == null) return;

        if (isNextAttack) 
        {
            intentIcon.sprite = attackSprite;
            Debug.Log("지렁이 다음 행동 예고: 공격 (칼 아이콘)");
        }
        else 
        {
            intentIcon.sprite = defendSprite;
            Debug.Log("지렁이 다음 행동 예고: 방어 (방패 아이콘)");
        }
    }

    // 외부(TurnManager 등)에서 호출할 수 있는 공개 메서드
    public IEnumerator PlayActionAnimation()
    {
        yield return StartCoroutine(isNextAttack ? AttackRoutine() : DefendRoutine());
        isNextAttack = !isNextAttack;
        UpdateIntentUI();
    }

    // 공격: 왼쪽으로 슈슉 돌진했다 돌아오기
    IEnumerator AttackRoutine()
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

    // 방어: 스페이스바 누르면 커졌다가 작아지는 연출
    IEnumerator DefendRoutine()
    {
        isActing = true;
        Debug.Log("지렁이가 방어 태세: 몸집이 커집니다!");

        float duration = 0.4f;
        float timer = 0;
        Vector3 normalScale = new Vector3(baseScale, baseScale, baseScale);
        Vector3 bigScale = normalScale * 1.4f; // 1.4배 커지기

        // 커지는 구간 (0 ~ 0.2초)
        while (timer <= duration / 2f)
        {
            float t = timer / (duration / 2f);
            transform.localScale = Vector3.Lerp(normalScale, bigScale, t);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.1f); // 잠깐 큰 상태 유지

        // 작아지는 구간 (0.2 ~ 0.4초)
        timer = 0;
        while (timer <= duration / 2f)
        {
            float t = timer / (duration / 2f);
            transform.localScale = Vector3.Lerp(bigScale, normalScale, t);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = normalScale;
        isActing = false;
    }
}