using UnityEngine;
using UnityEngine.UI;

// 휴식 씬: Act에 맞는 배경과 휴식 BGM, 휴식 버튼(체력 회복 + 이펙트 + 회복 모션, 노드당 한 번).
public class RestManager : MonoBehaviour
{
    [Header("Rest Settings")]
    [SerializeField] private int healAmount = 10; // 휴식할 때 회복량
    [SerializeField] private Button restButton;

    [Header("Effect Settings")]
    [SerializeField] private GameObject restEffectObject; // 휴식할 때 켤 이펙트 (씬에 미리 둔 파티클)

    [Header("Background Settings")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer; // Act마다 바꿀 배경 렌더러
    [SerializeField] private Sprite act1Background;
    [SerializeField] private Sprite act2Background;
    [SerializeField] private Sprite act3Background;

    private void Start()
    {
        // Act에 맞는 배경
        UpdateBackgroundImage();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBGM(SoundManager.Instance.restBackgroundSound);

        // 이펙트는 처음엔 꺼 둠
        if (restEffectObject != null)
            restEffectObject.SetActive(false);

        if (restButton != null)
        {
            restButton.onClick.AddListener(OnRestButtonClicked);

            // 이미 휴식했으면(휴식 후 저장한 세이브를 불러온 경우 등) 다시 휴식할 수 없음
            if (GameFlowData.hasRested)
                restButton.interactable = false;
        }
    }

    // 지금 Act의 휴식 배경으로 바꾼다
    private void UpdateBackgroundImage()
    {
        if (backgroundSpriteRenderer == null) return;

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

    // 휴식: 체력 회복 → 이펙트 + 회복 모션 → 버튼 막기
    private void OnRestButtonClicked()
    {
        if (StatManager.Instance != null)
        {
            StatManager.Instance.HealPlayer(healAmount);
            GameFlowData.hasRested = true;

            Debug.Log($"휴식 완료: 체력 {healAmount} 회복!");

            // 이펙트 켜고 플레이어 회복 모션
            if (restEffectObject != null)
            {
                restEffectObject.SetActive(true);

                if (PlayerSpineController.Instance != null)
                {
                    PlayerSpineController.Instance.StartCoroutine(PlayerSpineController.Instance.HealRoutine());
                }

                // 파티클이면 처음부터 다시 재생
                ParticleSystem ps = restEffectObject.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                    ps.Play();
                }
            }

            // 한 노드에서 한 번만
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