using TMPro;
using UnityEngine;

// 상단 바 왼쪽의 진행 정보 표시 (예: "Act 1 · 3층")
[RequireComponent(typeof(TMP_Text))]
public class ProgressInfoText : MonoBehaviour
{
    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (label == null) return;

        // currentFloor는 0부터 시작 (맵에서 노드를 고르기 전이면 -1)
        int floor = GameFlowData.currentFloor + 1;
        label.text = floor > 0
            ? $"Act {GameFlowData.currentAct} · {floor}층"
            : $"Act {GameFlowData.currentAct}";
    }
}
