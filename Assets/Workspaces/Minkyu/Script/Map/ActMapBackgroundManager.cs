using UnityEngine;
using UnityEngine.UI;

public class ActMapBackgroundManager : MonoBehaviour
{
    [Header("Map Background")]
    public Image backgroundImage;

    [Header("Act Backgrounds")]
    public Sprite act1Background;
    public Sprite act2Background;
    public Sprite act3Background;

    private void Start()
    {
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (backgroundImage == null)
        {
            Debug.LogError(
                "ActMapBackgroundManager : " +
                "Background Image가 연결되지 않았습니다."
            );

            return;
        }

        switch (GameFlowData.currentAct)
        {
            case 1:
                backgroundImage.sprite =
                    act1Background;

                Debug.Log("Act 1 맵 배경 적용");
                break;

            case 2:
                backgroundImage.sprite =
                    act2Background;

                Debug.Log("Act 2 맵 배경 적용");
                break;

            case 3:
                backgroundImage.sprite =
                    act3Background;

                Debug.Log("Act 3 맵 배경 적용");
                break;

            default:
                Debug.LogWarning(
                    "알 수 없는 Act입니다 : " +
                    GameFlowData.currentAct
                );

                break;
        }
    }
}