using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WormMonster : MonoBehaviour
{
    [Header("UI 연결")]
    public Image intentIcon; 
    public Sprite attackSprite; 
    public Sprite defendSprite; 

    [Header("설정")]
    private Vector3 originalPosition;
    private Vector3 originalScale; // 맨 처음 오브젝트의 크기를 그대로 저장
    private bool isActing = false;
    private bool isNextAttack = true; // true: 공격, false: 방어

    void Start()
    {
        // 맨 처음 유니티에 배치되어 있던 원래 위치와 크기를 그대로 기억합니다. (절대 임의로 키우지 않음)
        originalPosition = transform.position;
        originalScale = transform.localScale;

        UpdateIntentUI(); 
    }

    void Update()
    {
        // 1. 평소 숨쉬는 듯한 미세한 꿀렁임 (원래 크기 기준)
        if (!isActing)
        {
            float bounce = Mathf.Sin(Time.time * 2f) * 0.03f;
            transform.localScale = new Vector3(originalScale.x - bounce, originalScale.y + bounce, originalScale.z);
        }

        // 2. 스페이스바 입력 처리
        if (Input.GetKeyDown(KeyCode.Space) && !isActing)
        {
            ExecuteAction();
        }
    }

    void ExecuteAction()
    {
        if (isNextAttack)
        {
            // 공격: 슬라임과 똑같이 왼쪽으로 돌진 공격!
            StartCoroutine(AttackRoutine());
        }
        else
        {
            // 방어: 몸이 빵빵하게 커졌다가 작아지는 연출
            StartCoroutine(GrowDefendRoutine());
        }

        isNextAttack = !isNextAttack;
        UpdateIntentUI();
    }

    void UpdateIntentUI()
    {
        if (intentIcon == null) return;
        intentIcon.sprite = isNextAttack ? attackSprite : defendSprite;
    }

    // 🟢 공격: 슬라임과 똑같이 왼쪽으로 슈슉 돌진했다 돌아오기
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
        transform.localScale = originalScale;
        isActing = false;
    }

    // 🛡️ 방어: 제자리에서 몸이 커졌다가 작아지는 연출
    IEnumerator GrowDefendRoutine()
    {
        isActing = true;

        float timer = 0;
        while (timer <= 0.3f)
        {
            float progress = timer / 0.3f;
            float currentScaleMultiplier = Mathf.Lerp(1.0f, 1.3f, progress);
            transform.localScale = new Vector3(originalScale.x * currentScaleMultiplier, originalScale.y * currentScaleMultiplier, originalScale.z);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        timer = 0;
        while (timer <= 0.3f)
        {
            float progress = timer / 0.3f;
            float currentScaleMultiplier = Mathf.Lerp(1.3f, 1.0f, progress);
            transform.localScale = new Vector3(originalScale.x * currentScaleMultiplier, originalScale.y * currentScaleMultiplier, originalScale.z);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        isActing = false;
    }
}