using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DontDestroyCanvas : MonoBehaviour
{
    private static DontDestroyCanvas instance;
    [SerializeField] private Image brightnessOverlay;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 씬이 전환될 때마다 ApplyBrightness 함수가 자동 실행되도록 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 해제
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyBrightness();
    }

    public void ApplyBrightness()
    {
        if (brightnessOverlay == null)
            brightnessOverlay = GetComponentInChildren<Image>();

        if (brightnessOverlay != null)
        {
            // 저장된 밝기 불러오기 (기본값 1.0f)
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 1.0f);

            Color color = brightnessOverlay.color;
            color.a = (1.0f - savedBrightness) * 0.8f;
            brightnessOverlay.color = color;
        }
    }
}