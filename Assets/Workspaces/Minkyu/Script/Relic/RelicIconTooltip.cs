using UnityEngine;
using UnityEngine.EventSystems;

// 유물 아이콘에 붙어서, 마우스를 올리면 RelicIconUI에 설명창을 띄워 달라고 알린다.
public class RelicIconTooltip : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private RelicData relicData;
    private RelicIconUI relicIconUI;

    // 아이콘을 만들 때 유물 정보 연결
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
            transform as RectTransform
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
