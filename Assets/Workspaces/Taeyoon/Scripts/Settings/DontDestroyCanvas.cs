using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 화면 밝기 조절용 검은 덮개 캔버스. 씬이 바뀌어도 유지되고, 씬이 로드될 때마다 저장된 밝기를 다시 적용한다.
public class DontDestroyCanvas : MonoBehaviour
{
    private static DontDestroyCanvas instance;
    [SerializeField] private Image brightnessOverlay;

    // 처음 만들어진 하나 (설정 창의 밝기 슬라이더가 사용)
    public static DontDestroyCanvas Instance => instance;

    // 밝기 덮개 이미지
    public Image Overlay
    {
        get
        {
            if (brightnessOverlay == null)
                brightnessOverlay = GetComponentInChildren<Image>();
            return brightnessOverlay;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 씬이 바뀔 때마다 밝기를 다시 적용
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 씬 이벤트 구독 해제
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyBrightness();
    }

    // 저장된 밝기(0~1, 기본 1)에 맞춰 덮개를 어둡게 (밝기 1이면 투명, 0이면 80% 검정)
    public void ApplyBrightness()
    {
        if (brightnessOverlay == null)
            brightnessOverlay = GetComponentInChildren<Image>();

        if (brightnessOverlay != null)
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);

            Color color = brightnessOverlay.color;
            color.a = (1.0f - savedBrightness) * 0.8f;
            brightnessOverlay.color = color;
        }
    }
}