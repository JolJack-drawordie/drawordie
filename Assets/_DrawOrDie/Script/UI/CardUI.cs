using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

// 카드 타입을 구분하기 위한 열거형 추가
public enum CardType { Adjective, Gerund, Synergy }

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int CardID; // 홍성구 추가 : 카드 id 추가
    
    // 추가된 카드 속성
    public CardType cardType;
    public int Cost;
    public int Damage;

    [Header("UI 연결")]
    public TMP_Text nameText;
    public TMP_Text costText;
    public TMP_Text descText;

    public Vector3 originPos;
    public Quaternion originRot;
    public Vector3 targetPos;
    public Quaternion targetRot;
    private Vector3 targetScale = Vector3.one;

    private RectTransform rect;
    private CardDraggable cardDrag; // 홍성구 추가 : 카드 드래그 스크립트
    public bool isInSlot = false; // 홍성구 추가 : 카드가 슬롯에 있는지 확인하는 변수

    // ⭐ 추가됨: 카드의 원래 순서를 기억하는 변수
    public int siblingIndex; 

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        cardDrag = GetComponent<CardDraggable>(); // 홍성구 추가 : 카드 드래그 스크립트 가져오기
    }

    // 홍성구 수정 : 카드 id 추가 (카드 타입, 실제 비용, 데미지도 함께 받도록 수정)
    public void Setup(int id, string name, string cost, string desc, CardType type, int actualCost, int actualDamage) 
    {
        CardID = id; 
        nameText.text = name;
        costText.text = cost;
        descText.text = desc;
        
        cardType = type;
        Cost = actualCost;
        Damage = actualDamage;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInSlot) OnCardFocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OffCardFocus(); // 홍성구 변경 : OffCardFocus 호출
    }

    //홍성구 추가 함수 : 기존의 OnPointerEnter내의 코드 전체 옮김
    public void OnCardFocus()
    {
        targetScale = Vector3.one * 1.2f;
        targetPos = originPos + new Vector3(0, 40f, 0); // 선택된 카드는 위로
        targetRot = Quaternion.identity;

        transform.SetAsLastSibling(); // 선택된 카드를 맨 앞으로

        // ⭐ 핵심: 옆 카드들 밀어내기
        foreach (Transform child in transform.parent)
        {
            CardUI sibling = child.GetComponent<CardUI>();
            if (sibling != null && sibling != this && !sibling.isInSlot)
            {
                // 나보다 앞번호면 왼쪽(-40), 뒷번호면 오른쪽(+40)으로 밀어냅니다.
                float pushAmount = (sibling.siblingIndex < this.siblingIndex) ? -40f : 40f;
                sibling.targetPos = sibling.originPos + new Vector3(pushAmount, -10f, 0); // 살짝 옆+아래로 비켜줌
            }
        }
    }

    //홍성구 추가 함수 : 기존의 OnPointerExit내의 코드 전체 옮김
    public void OffCardFocus()
    {
        if (cardDrag != null && cardDrag.isDragging) return; // 홍성구 추가 : 드래그 중이라면 리턴

        targetScale = Vector3.one;
        targetPos = originPos;
        targetRot = originRot;

        transform.SetSiblingIndex(siblingIndex); // 원래 순서 복구

        foreach (Transform child in transform.parent)
        {
            CardUI sibling = child.GetComponent<CardUI>();
            if (sibling != null && sibling != this && !sibling.isInSlot)
            {
                sibling.targetPos = sibling.originPos;
            }
        }
    }

    void Update()
    {
        if (cardDrag != null && cardDrag.isDragging) return;
        if (isInSlot) return; // 슬롯 안에서는 위치 애니메이션 금지 (targetPos가 패 좌표계 값이라 이상한 위치로 날아감)

        rect.localPosition = Vector3.Lerp(rect.localPosition, targetPos, Time.deltaTime * 10f);
        rect.localRotation = Quaternion.Lerp(rect.localRotation, targetRot, Time.deltaTime * 10f);
        rect.localScale = Vector3.Lerp(rect.localScale, targetScale, Time.deltaTime * 10f);
    }
}