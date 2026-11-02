using UnityEngine;

// 적 의도 아이콘에 붙어서, 마우스가 아이콘 위에 있으면 IntentTooltip으로 설명을 띄운다.
// 의도 아이콘은 월드 스페이스 Canvas라 UI 이벤트 대신 매 프레임 마우스 위치로 직접 판정한다.
// EnemyController.Start에서 자동으로 붙인다.
public class IntentTooltipHover : MonoBehaviour
{
    private EnemyController enemy;
    private RectTransform iconRect;
    private Canvas canvas;
    private bool showing;

    public void Init(EnemyController owner)
    {
        enemy = owner;
        iconRect = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (enemy == null || iconRect == null)
        {
            SetHidden();
            return;
        }

        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        bool hovered = RectTransformUtility.RectangleContainsScreenPoint(iconRect, Input.mousePosition, cam);

        if (hovered)
        {
            IntentTooltip.Show(this, enemy.GetIntentTooltip(), iconRect, cam);
            showing = true;
        }
        else
        {
            SetHidden();
        }
    }

    private void SetHidden()
    {
        if (!showing) return;
        IntentTooltip.Hide(this);
        showing = false;
    }

    private void OnDisable()
    {
        IntentTooltip.Hide(this);
        showing = false;
    }
}
