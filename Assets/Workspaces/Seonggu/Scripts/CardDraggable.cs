using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 유니티 UI 이벤트를 처리하기 위해 필수!


// CanvasGroup 컴포넌트 없으면 가져오기
[RequireComponent(typeof(CanvasGroup))]
public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    // 드래그 실패 시 돌아갈 위치
    private Vector3 lastPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();

    }

    // 1. 카드를 클릭해서 드래그를 시작할 때
    public void OnBeginDrag(PointerEventData eventData)
    {
        lastPosition = rectTransform.position; // 현재 위치 기억

        canvasGroup.alpha = 0.6f;              // 드래그 중엔 살짝 투명하게
        canvasGroup.blocksRaycasts = false;    // 중요: 카드가 마우스를 통과하게 함 (슬롯 인식을 위해)

        // 드래그하는 카드가 다른 UI보다 위로 올라오게 설정
        transform.SetAsLastSibling();
    }

    // 2. 드래그 중일 때
    public void OnDrag(PointerEventData eventData)
    {
        // 캔버스의 스케일에 맞춰서 마우스 좌표를 카드 좌표로 변환
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    // 3. 드래그 중 마우스 버튼을 뗐을 때
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1.0f;           // 투명도 복구
        canvasGroup.blocksRaycasts = true;  // 마우스 다시 인식하게 복구

        //슬롯이 아닌 다른 곳에 둘 때 원래 위치로 돌아감
        rectTransform.position = lastPosition; 
    }
}