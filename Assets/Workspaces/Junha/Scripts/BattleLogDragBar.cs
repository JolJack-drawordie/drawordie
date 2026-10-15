using UnityEngine;
using UnityEngine.EventSystems;

// 전투 로그 위쪽 검은 바. 잡고 끌면 로그 패널 전체가 따라 움직인다 (화면 밖으로는 못 나감).
public class BattleLogDragBar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BattleLogUI logUI;

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (logUI != null) logUI.MoveBy(eventData.delta / logUI.CanvasScale);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (logUI != null) logUI.SaveLayout();
    }
}
