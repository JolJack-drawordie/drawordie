using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RelicIconUI : MonoBehaviour
{
    public static RelicIconUI Instance { get; private set; }

    [Header("유물 아이콘 UI")]
    [SerializeField] private Transform relicIconContainer;
    [SerializeField] private GameObject relicIconPrefab;

    [Header("유물별 아이콘 연결")]
    [SerializeField]
    private List<RelicIconEntry> relicIcons =
        new List<RelicIconEntry>();

    [Header("유물 툴팁 UI")]
    [SerializeField] private GameObject relicTooltip;
    [SerializeField] private TextMeshProUGUI relicTooltipName;
    [SerializeField] private TextMeshProUGUI relicTooltipDescription;
    [SerializeField] private Canvas rootCanvas;

    [Header("툴팁 위치")]
    [Tooltip("아이콘과 툴팁 사이 간격")]
    [SerializeField] private float tooltipGap = 10f;
    [SerializeField] private float screenPadding = 15f;

    [Serializable]
    public class RelicIconEntry
    {
        public string relicKey;
        public Sprite icon;
    }

    private readonly HashSet<string> displayedRelicKeys =
        new HashSet<string>();

    private RectTransform tooltipRect;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[RelicIconUI] 중복 인스턴스를 제거합니다."
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (relicTooltip != null)
        {
            relicTooltip.SetActive(false);

            tooltipRect =
                relicTooltip.GetComponent<RectTransform>();

            // 툴팁이 마우스 이벤트를 가로채지 않도록 설정
            Graphic[] graphics =
                relicTooltip.GetComponentsInChildren<Graphic>(true);

            foreach (Graphic graphic in graphics)
            {
                graphic.raycastTarget = false;
            }
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (rootCanvas != null)
        {
            canvasRect =
                rootCanvas.GetComponent<RectTransform>();
        }

        if (tooltipRect != null)
        {
            // 툴팁의 기준점을 중앙으로 설정
            tooltipRect.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    private IEnumerator Start()
    {
        Debug.Log("[RelicIconUI] 아이콘 초기화 시작");

        while (RelicManager.Instance == null)
        {
            yield return null;
        }

        while (RelicManager.Instance.AllRelics == null ||
               RelicManager.Instance.AllRelics.Count == 0)
        {
            yield return null;
        }

        RefreshRelicIcons();

        Debug.Log("[RelicIconUI] 아이콘 초기화 완료");
    }

    // 현재 보유한 유물 아이콘을 다시 표시
    public void RefreshRelicIcons()
    {
        if (relicIconContainer == null)
        {
            Debug.LogError(
                "[RelicIconUI] RelicIconContainer가 연결되지 않았습니다."
            );
            return;
        }

        if (relicIconPrefab == null)
        {
            Debug.LogError(
                "[RelicIconUI] 유물 아이콘 프리팹이 연결되지 않았습니다."
            );
            return;
        }

        if (RelicManager.Instance == null)
        {
            Debug.LogWarning(
                "[RelicIconUI] RelicManager를 찾을 수 없습니다."
            );
            return;
        }

        if (RelicManager.Instance.AllRelics == null)
        {
            Debug.LogWarning(
                "[RelicIconUI] 유물 목록이 아직 준비되지 않았습니다."
            );
            return;
        }

        HideRelicTooltip();

        foreach (Transform child in relicIconContainer)
        {
            Destroy(child.gameObject);
        }

        displayedRelicKeys.Clear();

        foreach (RelicData relic in RelicManager.Instance.AllRelics)
        {
            if (relic == null ||
                string.IsNullOrEmpty(relic.key))
            {
                continue;
            }

            if (!RelicManager.Instance.HasRelic(relic.key))
            {
                continue;
            }

            if (!displayedRelicKeys.Add(relic.key))
            {
                continue;
            }

            Sprite iconSprite = FindRelicIcon(relic.key);

            if (iconSprite == null)
            {
                Debug.LogWarning(
                    $"[RelicIconUI] 아이콘 스프라이트가 없습니다: {relic.key}"
                );

                displayedRelicKeys.Remove(relic.key);
                continue;
            }

            GameObject iconObject = Instantiate(
                relicIconPrefab,
                relicIconContainer
            );

            Image iconImage =
                iconObject.GetComponent<Image>();

            if (iconImage == null)
            {
                iconImage =
                    iconObject.GetComponentInChildren<Image>();
            }

            if (iconImage == null)
            {
                Debug.LogError(
                    "[RelicIconUI] 프리팹에서 Image 컴포넌트를 찾을 수 없습니다."
                );

                Destroy(iconObject);
                displayedRelicKeys.Remove(relic.key);
                continue;
            }

            iconImage.sprite = iconSprite;
            iconImage.enabled = true;
            iconImage.raycastTarget = true;

            iconObject.name = $"RelicIcon_{relic.key}";

            // 아이콘 호버 이벤트 연결
            RelicIconTooltip hoverHandler =
                iconObject.GetComponent<RelicIconTooltip>();

            if (hoverHandler == null)
            {
                hoverHandler =
                    iconObject.AddComponent<RelicIconTooltip>();
            }

            hoverHandler.Initialize(relic, this);

            Debug.Log(
                $"[RelicIconUI] 유물 아이콘 표시: {relic.name}"
            );
        }
    }

    // 유물 키에 맞는 스프라이트 검색
    private Sprite FindRelicIcon(string relicKey)
    {
        foreach (RelicIconEntry entry in relicIcons)
        {
            if (entry != null &&
                entry.relicKey == relicKey)
            {
                return entry.icon;
            }
        }

        return null;
    }

    // 유물 툴팁 표시 (아이콘 바로 위에 표시)
    public void ShowRelicTooltip(
        RelicData relic,
        RectTransform iconRect)
    {
        if (relic == null ||
            relicTooltip == null ||
            relicTooltipName == null ||
            relicTooltipDescription == null)
        {
            return;
        }

        if (rootCanvas == null || canvasRect == null ||
            tooltipRect == null)
        {
            Debug.LogError(
                "[RelicIconUI] Canvas 또는 Tooltip RectTransform이 연결되지 않았습니다."
            );
            return;
        }

        relicTooltipName.text = relic.name;
        relicTooltipDescription.text = relic.description;

        relicTooltip.SetActive(true);

        // 텍스트 변경 후 레이아웃 갱신
        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            tooltipRect
        );

        Canvas.ForceUpdateCanvases();

        UpdateTooltipPosition(iconRect);

        // 툴팁을 가장 위에 표시
        tooltipRect.SetAsLastSibling();
    }

    // 아이콘 위쪽 가운데에 툴팁 배치 (위쪽 공간이 부족하면 아이콘 아래로)
    private void UpdateTooltipPosition(RectTransform iconRect)
    {
        if (tooltipRect == null ||
            canvasRect == null ||
            iconRect == null)
        {
            return;
        }

        // 아이콘의 네 모서리를 Canvas 기준 좌표로 변환
        Vector3[] corners = new Vector3[4];
        iconRect.GetWorldCorners(corners);

        Vector2 bottomLeft = canvasRect.InverseTransformPoint(corners[0]);
        Vector2 topRight = canvasRect.InverseTransformPoint(corners[2]);

        Rect canvasBounds = canvasRect.rect;
        Vector2 tooltipSize = tooltipRect.rect.size;

        float halfWidth = tooltipSize.x * 0.5f;
        float halfHeight = tooltipSize.y * 0.5f;
        float centerX = (bottomLeft.x + topRight.x) * 0.5f;

        // 기본: 아이콘 위쪽
        float centerY = topRight.y + tooltipGap + halfHeight;

        // 위쪽 공간이 부족하면 아이콘 아래쪽
        if (centerY + halfHeight > canvasBounds.yMax - screenPadding)
        {
            centerY = bottomLeft.y - tooltipGap - halfHeight;
        }

        // 화면 밖으로 나가지 않도록 제한
        centerX = Mathf.Clamp(
            centerX,
            canvasBounds.xMin + halfWidth + screenPadding,
            canvasBounds.xMax - halfWidth - screenPadding
        );

        centerY = Mathf.Clamp(
            centerY,
            canvasBounds.yMin + halfHeight + screenPadding,
            canvasBounds.yMax - halfHeight - screenPadding
        );

        tooltipRect.anchoredPosition = new Vector2(centerX, centerY);
    }

    // 유물 툴팁 숨기기
    public void HideRelicTooltip()
    {
        if (relicTooltip != null)
        {
            relicTooltip.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
