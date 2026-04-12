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


    public CardSlot currentSlot; // 이 카드가 지금 들어있는 슬롯 (없으면 null)
    private Vector3 startPosition; // 드래그 시작 위치
    private RectTransform originalParent; // 원래 부모 (손패 위치 등)

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();

    }

    // 1. 카드를 클릭해서 드래그를 시작할 때
    public void OnBeginDrag(PointerEventData eventData)
    {
        //슬롯에 있는 카드라면 슬롯 비우기
        if (currentSlot != null)
        {
            currentSlot.RemoveCard();
            currentSlot = null;       
        }

        startPosition = transform.position; //현재 위치 기억
        originalParent = transform.parent as RectTransform; // 원래 있었던 부모 위치 ex) 손패


        transform.SetParent(canvas.transform); //슬롯을 인식하기 위해 캔버스의 자식으로 올림
        transform.SetAsLastSibling(); // 캔버스 내에서 가장 마지막 자식이 되어 화면 앞으로 나오게

        canvasGroup.alpha = 0.6f;              // 드래그 중엔 살짝 투명하게
        canvasGroup.blocksRaycasts = false;    // 카드가 마우스를 통과하게 함
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

        if (transform.parent == canvas.transform) //슬롯에 안들어갔다면 
        {
            // 원래 위치로 복귀!
            transform.SetParent(originalParent);
            transform.position = startPosition;
        }
    }
}