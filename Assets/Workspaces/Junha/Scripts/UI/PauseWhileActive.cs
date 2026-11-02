using UnityEngine;
using UnityEngine.UI;

// 이 오브젝트(설정 창)가 켜져 있는 동안 게임을 일시정지하고, 화면 전체를 덮는 어두운 배경으로 뒤쪽 클릭(카드 드래그, 턴 종료 등)을 막는다.
// 설정 창을 여는 방식(로비는 LobbyManager, 휴식 / 전투는 SettingsManager)과 관계없이 동작하도록 패널 자체에 붙인다.
public class PauseWhileActive : MonoBehaviour
{
    private static readonly Color BlockerColor = new Color(0f, 0f, 0f, 0.5f);
    private const float BlockerSize = 10000f; // 어떤 해상도 / 부모 크기에서도 화면을 덮도록 충분히 크게

    private GameObject blocker;

    private void OnEnable()
    {
        GamePause.Pause();
        ShowBlocker(true);
    }

    private void OnDisable()
    {
        GamePause.Resume();
        ShowBlocker(false);
    }

    private void OnDestroy()
    {
        if (blocker != null) Destroy(blocker);
    }

    private void ShowBlocker(bool show)
    {
        if (show && blocker == null) CreateBlocker();
        if (blocker == null) return;

        // 씬 전환 등으로 부모째 꺼지는 중에는 건드리지 않음 (어차피 같이 꺼짐)
        if (!show && (transform.parent == null || !transform.parent.gameObject.activeInHierarchy)) return;

        blocker.SetActive(show);
        if (!show) return;

        // 패널 바로 뒤에 그려지도록 형제로 둔다 (패널 자식으로 두면 패널 배경 그림까지 덮음)
        int panelIndex = transform.GetSiblingIndex();
        int blockerIndex = blocker.transform.GetSiblingIndex();
        blocker.transform.SetSiblingIndex(blockerIndex < panelIndex ? panelIndex - 1 : panelIndex);
    }

    // 클릭을 막는 반투명 검은 배경 (패널의 형제 오브젝트)
    private void CreateBlocker()
    {
        if (transform.parent == null) return;

        blocker = new GameObject("PauseBlocker", typeof(RectTransform));
        blocker.layer = gameObject.layer;
        blocker.transform.SetParent(transform.parent, false);

        RectTransform rect = (RectTransform)blocker.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(BlockerSize, BlockerSize);

        Image image = blocker.AddComponent<Image>();
        image.color = BlockerColor;
        image.raycastTarget = true;
    }
}
