using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UI;

public class RestManager : MonoBehaviour
{
    [Header("Rest Settings")]
    [SerializeField] private int healAmount = 10; // 휴식 시 회복량
    [SerializeField] private Button restButton;   // 휴식하기 버튼

    private void Start()
    {
        if (restButton != null)
        {
            restButton.onClick.AddListener(OnRestButtonClicked);
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

            Debug.Log($"휴식 완료: 체력 {healAmount} 회복!");

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