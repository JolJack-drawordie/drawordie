using UnityEngine;

public class MapLineDrawer : MonoBehaviour
{
    public GameObject linePrefab;
    public Transform lineParent;

    public void DrawLine(RectTransform from, RectTransform to)
    {
        GameObject line =
            Instantiate(linePrefab, lineParent);

        RectTransform rect =
            line.GetComponent<RectTransform>();

        Vector2 start = from.anchoredPosition;
        Vector2 end = to.anchoredPosition;

        Vector2 dir = end - start;

        rect.anchoredPosition =
            (start + end) / 2f;

        rect.sizeDelta =
            new Vector2(dir.magnitude, 6f);

        float angle =
            Mathf.Atan2(dir.y, dir.x)
            * Mathf.Rad2Deg;

        rect.localRotation =
            Quaternion.Euler(0, 0, angle);
    }
}