using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button lordGameButton;
    [SerializeField] private Button settingsButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Image fadeImage;

    private bool isTransitioning;

    void Awake()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (fadeImage     != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }

    void Start()
    {
        if (newGameButton == null)
            Debug.LogError("[LobbyManager] newGameButton 미연결!");
        else
        {
            newGameButton.interactable = true;
            newGameButton.onClick.AddListener(OnNewGameClick);
            Debug.Log("[LobbyManager] NewGame 버튼 이벤트 등록 완료");
        }

        if (lordGameButton == null)
            Debug.LogError("[LobbyManager] lordGameButton 미연결!");
        else
        {
            lordGameButton.interactable = true;
            lordGameButton.onClick.AddListener(OnLordGameClick);
            Debug.Log("[LobbyManager] LordGame 버튼 이벤트 등록 완료");
        }

        if (settingsButton == null)
            Debug.LogError("[LobbyManager] settingsButton 미연결!");
        else
        {
            settingsButton.interactable = true;
            settingsButton.onClick.AddListener(OnSettingsClick);
            Debug.Log("[LobbyManager] Settings 버튼 이벤트 등록 완료");
        }
    }

    public void OnNewGameClick()
    {
        Debug.Log("[LobbyManager] NewGame 클릭됨");
        if (isTransitioning) return;
        StartCoroutine(FadeAndLoad("MapScene"));
    }

    public void OnLordGameClick()
    {
        Debug.Log("[LobbyManager] LordGame 클릭됨");
    }

    public void OnSettingsClick()
    {
        Debug.Log("[LobbyManager] Settings 클릭됨");
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        isTransitioning = true;

        if (fadeImage != null)
        {
            float timer = 0f;
            while (timer < 0.8f)
            {
                timer += Time.deltaTime;
                fadeImage.color = new Color(0f, 0f, 0f, timer / 0.8f);
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneName);
        // 배경 음악 정지
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
        }
    }
}
