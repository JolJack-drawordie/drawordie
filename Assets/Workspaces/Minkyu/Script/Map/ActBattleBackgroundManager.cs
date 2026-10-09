using UnityEngine;

public class ActBattleBackgroundManager : MonoBehaviour
{
    [Header("Background Renderers")]
    public SpriteRenderer topRenderer;
    public SpriteRenderer floorRenderer;

    [Header("Act 1")]
    public Sprite act1Top;
    public Sprite act1Floor;

    public Vector3 act1TopPosition;
    public Vector3 act1TopScale;

    public Vector3 act1FloorPosition;
    public Vector3 act1FloorScale;

    [Header("Act 2")]
    public Sprite act2Top;
    public Sprite act2Floor;

    public Vector3 act2TopPosition;
    public Vector3 act2TopScale;

    public Vector3 act2FloorPosition;
    public Vector3 act2FloorScale;

    [Header("Act 3")]
    public Sprite act3Top;
    public Sprite act3Floor;

    public Vector3 act3TopPosition;
    public Vector3 act3TopScale;

    public Vector3 act3FloorPosition;
    public Vector3 act3FloorScale;


    private void Start()
    {
        ApplyActBackground();
    }


    public void ApplyActBackground()
    {
        int currentAct = GameFlowData.currentAct;

        switch (currentAct)
        {
            case 1:
                ApplyAct(
                    act1Top,
                    act1Floor,
                    act1TopPosition,
                    act1TopScale,
                    act1FloorPosition,
                    act1FloorScale
                );
                break;

            case 2:
                ApplyAct(
                    act2Top,
                    act2Floor,
                    act2TopPosition,
                    act2TopScale,
                    act2FloorPosition,
                    act2FloorScale
                );
                break;

            case 3:
                ApplyAct(
                    act3Top,
                    act3Floor,
                    act3TopPosition,
                    act3TopScale,
                    act3FloorPosition,
                    act3FloorScale
                );
                break;

            default:
                Debug.LogWarning(
                    $"지원하지 않는 Act입니다 : {currentAct}"
                );
                break;
        }

        Debug.Log(
            $"Act {currentAct} 전투 배경 적용 완료"
        );
    }


    private void ApplyAct(
        Sprite topSprite,
        Sprite floorSprite,
        Vector3 topPosition,
        Vector3 topScale,
        Vector3 floorPosition,
        Vector3 floorScale
    )
    {
        if (topRenderer == null)
        {
            Debug.LogError(
                "ActBattleBackgroundManager : Top Renderer가 연결되지 않았습니다."
            );
            return;
        }

        if (floorRenderer == null)
        {
            Debug.LogError(
                "ActBattleBackgroundManager : Floor Renderer가 연결되지 않았습니다."
            );
            return;
        }


        // -------------------------
        // Top
        // -------------------------

        topRenderer.sprite = topSprite;

        topRenderer.transform.localPosition = topPosition;
        topRenderer.transform.localScale = topScale;


        // -------------------------
        // Floor
        // -------------------------

        floorRenderer.sprite = floorSprite;

        floorRenderer.transform.localPosition = floorPosition;
        floorRenderer.transform.localScale = floorScale;
    }
}