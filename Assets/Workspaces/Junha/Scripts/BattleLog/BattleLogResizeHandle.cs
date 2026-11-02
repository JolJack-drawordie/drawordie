using UnityEngine;
using UnityEngine.EventSystems;

// 전투 로그 패널의 가장자리(왼쪽 / 오른쪽 / 아래, 아래 모서리). 끌면 그 방향으로 패널 크기가 바뀐다.
public class BattleLogResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BattleLogUI logUI;
    public int horizontal; // -1 왼쪽 변, +1 오른쪽 변, 0 없음
    public int vertical; // -1 아래 변, 0 없음

    // 드래그 이벤트를 받으려면 IBeginDragHandler 구현이 필요해서 비워 둔다
    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (logUI != null) logUI.ResizeBy(eventData.delta / logUI.CanvasScale, horizontal, vertical);
    }

    // 놓으면 크기를 저장
    public void OnEndDrag(PointerEventData eventData)
    {
        if (logUI != null) logUI.SaveLayout();
    }
}
