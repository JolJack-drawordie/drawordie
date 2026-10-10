using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;

public class RestManager : MonoBehaviour
{
    [Header("Rest Settings")]
    [SerializeField] private int healAmount = 10; // 휴식 시 회복량
    [SerializeField] private Button restButton;   // 휴식하기 버튼

    [Header("Effect Settings")]
    [SerializeField] private GameObject restEffectObject; // 1. 씬에 미리 배치해둔 파티클 오브젝트 (또는 프리팹)

    [Header("Background Settings")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer; // 배경 오브젝트의 SpriteRenderer 컴포넌트
    [SerializeField] private Sprite act1Background; // Act 1 휴식 배경 스프라이트
    [SerializeField] private Sprite act2Background; // Act 2 휴식 배경 스프라이트
    [SerializeField] private Sprite act3Background; // Act 3 휴식 배경 스프라이트

    private void Start()
    {
        // Act에 따른 배경 오브젝트 이미지(스프라이트) 교체 적용
        UpdateBackgroundImage();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(SoundManager.Instance.restBackgroundSound);

        // 시작할 때 파티클이 켜져있다면 꺼두기
        if (restEffectObject != null)
            restEffectObject.SetActive(false);

        if (restButton != null)
        {
            restButton.onClick.AddListener(OnRestButtonClicked);

            // 이미 휴식한 노드(휴식 후 저장한 세이브를 불러온 경우 등)면 다시 휴식할 수 없음
            if (GameFlowData.hasRested)
                restButton.interactable = false;
        }
    }

    private void UpdateBackgroundImage()
    {
        if (backgroundSpriteRenderer == null) return;

        // 현재 Act 정보를 가져오는 방식 (프로젝트 구조에 맞게 수정 가능)
        int currentAct = GameFlowData.currentAct;

        switch (currentAct)
        {
            case 1:
                if (act1Background != null) backgroundSpriteRenderer.sprite = act1Background;
                break;
            case 2:
                if (act2Background != null) backgroundSpriteRenderer.sprite = act2Background;
                break;
            case 3:
                if (act3Background != null) backgroundSpriteRenderer.sprite = act3Background;
                break;
            default:
                Debug.LogWarning($"알 수 없는 Act 번호입니다: {currentAct}");
                break;
        }
    }

    private void OnRestButtonClicked()
    {
        // StatManager나 플레이어 데이터를 통해 체력 회복 로직 수행
        if (StatManager.Instance != null)
        {
            // 예시: StatManager에 플레이어 체력을 회복시키는 메서드가 있다고 가정
            // StatManager.Instance.HealPlayer(healAmount);
            StatManager.Instance.HealPlayer(healAmount);
            GameFlowData.hasRested = true;

            Debug.Log($"휴식 완료: 체력 {healAmount} 회복!");

            // 2. 휴식 버튼을 누를 때 파티클 켜기
            if (restEffectObject != null)
            {
                restEffectObject.SetActive(true);

                if (PlayerSpineController.Instance != null)
                {
                    PlayerSpineController.Instance.StartCoroutine(PlayerSpineController.Instance.HealRoutine());
                }

                // 만약 ParticleSystem 컴포넌트가 직접 붙어있다면 확실하게 Play() 호출
                ParticleSystem ps = restEffectObject.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                    ps.Play();
                }

            }

            // 버튼 비활성화 (중복 휴식 방지 등)
            if (restButton != null)
            {
                restButton.interactable = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (restButton != null)
        {
            restButton.onClick.RemoveListener(OnRestButtonClicked);
        }
    }
}