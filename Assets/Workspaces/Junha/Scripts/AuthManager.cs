using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class AuthManager : MonoBehaviour
{
    [Header("패널 오브젝트")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("로그인 입력 필드")]
    public TMP_InputField loginID;
    public TMP_InputField loginPW;

    [Header("회원가입 입력 필드")]
    public TMP_InputField regID;
    public TMP_InputField regPW;
    public TMP_InputField regNick;

    private const string AuthSceneName = "AuthScene";
    private const string NextSceneName = "MapScene";

    void Start()
    {
        // Additive 로드 시 TitleScreen을 덮지 않도록
        // AuthScene 전용 카메라와 오디오 리스너를 비활성화
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            Camera cam = root.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.enabled = false;

            AudioListener al = root.GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;
        }

        // Canvas를 최상단에 렌더링
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
            }
        }
    }

    public void ClosePopup()
    {
        SceneManager.UnloadSceneAsync(AuthSceneName);
    }

    // 화면 전환 기능 
    public void OpenRegister() { 
        loginPanel.SetActive(false); 
        registerPanel.SetActive(true); 
    }
    public void OpenLogin() { 
        registerPanel.SetActive(false); 
        loginPanel.SetActive(true); 
    }

    // 버튼 클릭 이벤트
    public void LoginClick() { 
        StartCoroutine(LoginAction()); 
    }
    public void RegisterClick() { 
        StartCoroutine(RegisterAction());
    }

    IEnumerator LoginAction()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", loginID.text);
        form.AddField("password", loginPW.text);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:8080/api/users/login", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                Debug.Log("<color=green>로그인 성공!</color>");
                DOTween.KillAll();
                SceneManager.LoadScene(NextSceneName);
            } else {
                Debug.LogError("로그인 실패: " + www.error);
                // 로그인 실패 시 팝업을 닫지 않고 그대로 두어 사용자가 다시 입력할 수 있게 합니다.
            }
        }
    }

    IEnumerator RegisterAction()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", regID.text);
        form.AddField("password", regPW.text);
        form.AddField("nickname", regNick.text);

        using (UnityWebRequest www = UnityWebRequest.Post("http://localhost:8080/api/users/register", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                Debug.Log("<color=green>회원가입 성공!</color>");
                OpenLogin(); // 가입 성공 시 자동으로 로그인 패널로 전환
            } else {
                Debug.LogError("회원가입 실패: " + www.error);
            }
        }
    }
}