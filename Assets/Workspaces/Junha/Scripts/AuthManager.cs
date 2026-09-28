using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class LoginResponseData
{
    public long userId;
    public string username;
    public string nickname;
}

public class AuthManager : MonoBehaviour
{
    // 로그인한 유저 정보 (씬이 전환/언로드돼도 유지되도록 static으로 보관)
    public static long userId;
    public static string nickname;
    public static bool isLoggedIn = false;

    [Header("패널 오브젝트")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("로그인 입력 필드")]
    public TMP_InputField loginID;
    public TMP_InputField loginPW;

    [Header("회원가입 입력 필드")]
    public TMP_InputField regID;
    public TMP_InputField regPW;
    public TMP_InputField regPWConfirm;
    public TMP_InputField regNick;

    [Header("회원가입 보조 UI")]
    public TMP_Text regMessage;
    public TMP_Text togglePasswordButtonLabel;

    private bool isUsernameChecked = false;
    private string checkedUsername = "";
    private bool isPasswordVisible = false;

    private const string AuthSceneName = "AuthScene";
    private const string NextSceneName = "LobbyScene";
    private const string ApiBaseUrl = "http://localhost:8080";

    void Start()
    {
        // 아이디를 다시 수정하면 중복확인을 다시 받아야 함
        if (regID != null)
        {
            regID.onValueChanged.AddListener(_ => isUsernameChecked = false);
        }

        // 로그인 비밀번호도 마스킹
        if (loginPW != null)
        {
            loginPW.contentType = TMP_InputField.ContentType.Password;
            loginPW.ForceLabelUpdate();
        }

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
        if (!ValidateRegisterInput()) return;
        StartCoroutine(RegisterAction());
    }

    public void CheckUsernameDuplicateClick() {
        if (string.IsNullOrWhiteSpace(regID.text)) {
            ShowRegMessage("아이디를 입력해주세요.", false);
            return;
        }
        StartCoroutine(CheckUsernameDuplicateAction());
    }

    public void TogglePasswordVisibilityClick() {
        isPasswordVisible = !isPasswordVisible;

        TMP_InputField.ContentType contentType =
            isPasswordVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;

        if (regPW != null) {
            regPW.contentType = contentType;
            regPW.ForceLabelUpdate();
        }
        if (regPWConfirm != null) {
            regPWConfirm.contentType = contentType;
            regPWConfirm.ForceLabelUpdate();
        }
        if (togglePasswordButtonLabel != null) {
            togglePasswordButtonLabel.text = isPasswordVisible ? "숨기기" : "보기";
        }
    }

    IEnumerator LoginAction()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", loginID.text);
        form.AddField("password", loginPW.text);

        using (UnityWebRequest www = UnityWebRequest.Post(ApiBaseUrl + "/api/users/login", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                LoginResponseData response = JsonUtility.FromJson<LoginResponseData>(www.downloadHandler.text);

                userId = response.userId;
                nickname = response.nickname;
                isLoggedIn = true;

                Debug.Log($"<color=green>로그인 성공! 유저 번호: {userId}</color>");
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

        using (UnityWebRequest www = UnityWebRequest.Post(ApiBaseUrl + "/api/users/register", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                Debug.Log("<color=green>회원가입 성공!</color>");
                ShowRegMessage("", true);
                OpenLogin(); // 가입 성공 시 자동으로 로그인 패널로 전환
            } else {
                Debug.LogError("회원가입 실패: " + www.error);
                ShowRegMessage("회원가입에 실패했습니다.", false);
            }
        }
    }

    IEnumerator CheckUsernameDuplicateAction()
    {
        string url = ApiBaseUrl + "/api/users/check-username?username=" + UnityWebRequest.EscapeURL(regID.text);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                bool isDuplicate = www.downloadHandler.text.Trim().ToLower() == "true";

                if (isDuplicate) {
                    isUsernameChecked = false;
                    ShowRegMessage("이미 사용중인 아이디입니다.", false);
                } else {
                    isUsernameChecked = true;
                    checkedUsername = regID.text;
                    ShowRegMessage("사용 가능한 아이디입니다.", true);
                }
            } else {
                Debug.LogError("아이디 중복 확인 실패: " + www.error);
                ShowRegMessage("중복 확인에 실패했습니다.", false);
            }
        }
    }

    private bool ValidateRegisterInput()
    {
        if (string.IsNullOrWhiteSpace(regID.text) || string.IsNullOrWhiteSpace(regPW.text) || string.IsNullOrWhiteSpace(regNick.text)) {
            ShowRegMessage("모든 항목을 입력해주세요.", false);
            return false;
        }

        if (!isUsernameChecked || checkedUsername != regID.text) {
            ShowRegMessage("아이디 중복 확인을 해주세요.", false);
            return false;
        }

        if (regPWConfirm != null && regPW.text != regPWConfirm.text) {
            ShowRegMessage("비밀번호가 일치하지 않습니다.", false);
            return false;
        }

        return true;
    }

    private void ShowRegMessage(string message, bool success)
    {
        if (regMessage == null) return;
        regMessage.text = message;
        regMessage.color = success ? new Color(0.35f, 0.85f, 0.35f) : new Color(0.9f, 0.3f, 0.25f);
    }
}