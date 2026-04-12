using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public bool isOccupied = false; // 이미 슬롯에 카드가 있는지 체크
    public GameObject currentCard = null; // 현재 슬롯에 있는 카드 저장

    // 카드가 슬롯에 들어왔을 때 호출될 함수
    public void PlaceCard(GameObject card)
    {
        currentCard = card;
        isOccupied = true;

        card.transform.position = transform.position;
        card.transform.SetParent(transform); // 슬롯의 자식으로 설정
        card.transform.localPosition = Vector3.zero; // 슬롯의 중앙에 배치
    }

    public void RemoveCard()
    {
        currentCard = null;
        isOccupied = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;

        if (isOccupied) return; //슬롯에 이미 카드가 있다면 실패

        if (dropped != null)
        {
            PlaceCard(dropped);
        }
    }
}
