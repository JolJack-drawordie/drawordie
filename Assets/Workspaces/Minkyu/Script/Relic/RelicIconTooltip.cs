using UnityEngine;
using UnityEngine.EventSystems;

public class RelicIconTooltip : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private RelicData relicData;
    private RelicIconUI relicIconUI;

    // 아이콘 생성 시 유물 정보 연결
    public void Initialize(
        RelicData relic,
        RelicIconUI iconUI)
    {
        relicData = relic;
        relicIconUI = iconUI;
    }

    // 마우스가 아이콘 위에 올라왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (relicIconUI == null || relicData == null)
            return;

        relicIconUI.ShowRelicTooltip(
            relicData,
            eventData
        );
    }

    // 마우스가 아이콘에서 벗어났을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        if (relicIconUI == null)
            return;

        relicIconUI.HideRelicTooltip();
    }
}
