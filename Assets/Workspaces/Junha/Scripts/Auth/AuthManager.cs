using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;

// 서버 로그인 응답 (JSON)
[System.Serializable]
public class LoginResponseData
{
    public long userId;
    public string username;
    public string nickname;
    public string token; // 서버가 발급한 JWT
}

// 로그인 / 회원가입 팝업(AuthScene). 타이틀 화면 위에 Additive로 열린다.
// 로그인에 성공하면 유저 정보와 토큰을 static으로 보관하고 로비로 이동한다.
public class AuthManager : MonoBehaviour
{
    // 로그인 정보는 씬이 바뀌어도 유지되도록 static으로 보관
    public static long userId;
    public static string token; // 세이브/로드/결과 등록 요청의 Authorization 헤더에 쓴다
    public static bool isLoggedIn = false;

    // 로그아웃: 보관한 로그인 정보만 지운다 (JWT 방식이라 서버에 알릴 필요 없음)
    public static void Logout()
    {
        userId = 0;
        token = null;
        isLoggedIn = false;
    }

    // 로그인이 필요한 서버 요청에 "Authorization: Bearer <토큰>" 헤더를 붙인다
    public static void SetAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(token))
            request.SetRequestHeader("Authorization", "Bearer " + token);
    }

    [Header("패널 오브젝트")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("로그인 입력 필드")]
    public TMP_InputField loginID;
    public TMP_InputField loginPW;
    public TMP_Text loginMessage; // 로그인 버튼 위 안내 (빈 항목, 로그인 실패)

    [Header("회원가입 입력 필드")]
    public TMP_InputField regID;
    public TMP_InputField regPW;
    public TMP_InputField regPWConfirm;
    public TMP_InputField regNick;

    [Header("회원가입 보조 UI")]
    [FormerlySerializedAs("regMessage")] // 예전 필드 이름에 연결돼 있던 값을 그대로 이어받음
    public TMP_Text regIdMessage; // 아이디 입력칸 아래 (중복 확인 결과)
    public TMP_Text regPwMessage; // 비밀번호 입력칸 아래 (비밀번호 불일치)
    public TMP_Text regNickMessage; // 닉네임 입력칸 아래 (중복 확인 결과)
    public TMP_Text regSubmitMessage; // 회원가입 버튼 위 (빈 항목, 가입 실패)
    public TMP_Text togglePasswordButtonLabel;

    // 중복 확인을 통과한 아이디/닉네임. 확인 뒤에 입력을 바꾸면 다시 확인해야 한다
    private bool isUsernameChecked = false;
    private string checkedUsername = "";
    private bool isNicknameChecked = false;
    private string checkedNickname = "";
    private bool isPasswordVisible = false;
    private bool isLoggingIn = false; // 로그인 요청 중 중복 요청 방지

    private const string AuthSceneName = "AuthScene";
    private const string NextSceneName = "LobbyScene";
    private static string ApiBaseUrl => ApiConfig.BaseUrl;

    void Start()
    {
        // 아이디/닉네임을 고치면 이전 중복 확인 결과는 무효
        if (regID != null)
        {
            regID.onValueChanged.AddListener(_ =>
            {
                isUsernameChecked = false;
                ShowRegMessage(regIdMessage, "", true);
            });
        }

        if (regNick != null)
        {
            regNick.onValueChanged.AddListener(_ =>
            {
                isNicknameChecked = false;
                ShowRegMessage(regNickMessage, "", true);
            });
        }

        // 비밀번호나 로그인 정보를 다시 입력하면 이전 오류 메시지를 지운다
        if (regPW != null) regPW.onValueChanged.AddListener(_ => ShowRegMessage(regPwMessage, "", true));
        if (regPWConfirm != null) regPWConfirm.onValueChanged.AddListener(_ => ShowRegMessage(regPwMessage, "", true));

        if (loginID != null) loginID.onValueChanged.AddListener(_ => ShowRegMessage(loginMessage, "", true));
        if (loginPW != null) loginPW.onValueChanged.AddListener(_ => ShowRegMessage(loginMessage, "", true));

        // 로그인 비밀번호도 가려서 표시
        if (loginPW != null)
        {
            loginPW.contentType = TMP_InputField.ContentType.Password;
            loginPW.ForceLabelUpdate();
        }

        // 타이틀 화면 위에 Additive로 열리므로, 이 씬의 카메라/오디오 리스너는 꺼서 타이틀 화면을 가리지 않게 한다
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            Camera cam = root.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.enabled = false;

            AudioListener al = root.GetComponentInChildren<AudioListener>(true);
            if (al != null) al.enabled = false;
        }

        // 팝업 UI가 타이틀 UI보다 위에 그려지도록
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
            }
        }

        FocusFirstField(loginPanel);
    }

    void Update()
    {
        // Tab: 다음 입력칸 / Shift+Tab: 이전 입력칸
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            MoveFocus(reverse);
        }

        // Enter: 로그인 창이 열려 있으면 로그인
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) &&
            loginPanel != null && loginPanel.activeInHierarchy)
        {
            LoginClick();
        }
    }

    // 지금 열린 패널의 입력칸들 (Tab 이동 순서)
    private TMP_InputField[] GetActiveFields()
    {
        if (registerPanel != null && registerPanel.activeInHierarchy)
            return new[] { regID, regPW, regPWConfirm, regNick };
        if (loginPanel != null && loginPanel.activeInHierarchy)
            return new[] { loginID, loginPW };
        return new TMP_InputField[0];
    }

    // 선택된 칸이 없으면 첫(마지막) 칸으로, 있으면 다음(이전) 칸으로. 끝에서는 반대쪽 끝으로 돌아간다
    private void MoveFocus(bool reverse)
    {
        TMP_InputField[] fields = GetActiveFields();
        if (fields.Length == 0) return;

        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        int current = -1;
        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] != null && fields[i].gameObject == selected)
            {
                current = i;
                break;
            }
        }

        for (int step = 1; step <= fields.Length; step++)
        {
            int next = current < 0
                ? (reverse ? fields.Length - step : step - 1)
                : ((current + (reverse ? -step : step)) % fields.Length + fields.Length) % fields.Length;

            TMP_InputField field = fields[next];
            if (field != null && field.gameObject.activeInHierarchy && field.interactable)
            {
                field.Select();
                field.ActivateInputField();
                return;
            }
        }
    }

    // 패널을 열 때 첫 입력칸에 바로 입력할 수 있게 선택
    private void FocusFirstField(GameObject panel)
    {
        if (panel == null || !panel.activeInHierarchy) return;

        TMP_InputField[] fields = GetActiveFields();
        if (fields.Length > 0 && fields[0] != null)
        {
            fields[0].Select();
            fields[0].ActivateInputField();
        }
    }

    // 로그인 ↔ 회원가입 전환 시 입력값과 중복 확인 상태를 모두 초기화
    private void ClearInputs()
    {
        foreach (TMP_InputField field in new[] { loginID, loginPW, regID, regPW, regPWConfirm, regNick })
        {
            if (field != null) field.text = "";
        }

        isUsernameChecked = false;
        checkedUsername = "";
        isNicknameChecked = false;
        checkedNickname = "";
        ClearRegMessages();

        if (isPasswordVisible) TogglePasswordVisibilityClick();
    }

    // ===== 버튼 이벤트 (AuthScene 버튼 onClick에 연결) =====
    // 팝업 닫기 (타이틀로 돌아감)
    public void ClosePopup()
    {
        SceneManager.UnloadSceneAsync(AuthSceneName);
    }

    public void OpenRegister() { 
        ClearInputs();
        loginPanel.SetActive(false); 
        registerPanel.SetActive(true); 
        FocusFirstField(registerPanel);
    }
    public void OpenLogin() { 
        ClearInputs();
        registerPanel.SetActive(false); 
        loginPanel.SetActive(true); 
        FocusFirstField(loginPanel);
    }

    public void LoginClick() {  // Enter 연타 / 버튼 중복 클릭 방지
        if (isLoggingIn) return;

        if (string.IsNullOrWhiteSpace(loginID.text) || string.IsNullOrWhiteSpace(loginPW.text)) {
            ShowRegMessage(loginMessage, "아이디와 비밀번호를 입력해주세요.", false);
            return;
        }

        ShowRegMessage(loginMessage, "", true);
        StartCoroutine(LoginAction());
    }
    // 입력 검사(빈칸, 중복 확인, 비밀번호 일치)를 통과하면 가입 요청
    public void RegisterClick() {
        ShowRegMessage(regPwMessage, "", true);
        ShowRegMessage(regSubmitMessage, "", true);
        if (!ValidateRegisterInput()) return;
        StartCoroutine(RegisterAction());
    }

    public void CheckUsernameDuplicateClick() {
        if (string.IsNullOrWhiteSpace(regID.text)) {
            ShowRegMessage(regIdMessage, "아이디를 입력해주세요.", false);
            return;
        }
        StartCoroutine(CheckUsernameDuplicateAction());
    }

    public void CheckNicknameDuplicateClick() {
        if (string.IsNullOrWhiteSpace(regNick.text)) {
            ShowRegMessage(regNickMessage, "닉네임을 입력해주세요.", false);
            return;
        }
        StartCoroutine(CheckNicknameDuplicateAction());
    }

    // 회원가입 비밀번호 두 칸을 보이기 / 가리기
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

    // ===== 서버 요청 =====
    // 로그인 성공 시 토큰을 보관하고 로비로 이동. 실패하면 팝업을 유지하고 원인을 안내한다
    IEnumerator LoginAction()
    {
        isLoggingIn = true;

        WWWForm form = new WWWForm();
        form.AddField("username", loginID.text);
        form.AddField("password", loginPW.text);

        using (UnityWebRequest www = UnityWebRequest.Post(ApiBaseUrl + "/api/users/login", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                LoginResponseData response = JsonUtility.FromJson<LoginResponseData>(www.downloadHandler.text);

                userId = response.userId;
                token = response.token;
                isLoggedIn = true;

                Debug.Log($"<color=green>로그인 성공! 유저 번호: {userId}</color>");
                DOTween.KillAll(); // 타이틀 화면의 연출 트윈이 씬 전환 뒤에 남지 않도록 정리
                SceneManager.LoadScene(NextSceneName);
            } else {
                Debug.LogError("로그인 실패: " + www.error);
                ShowRegMessage(loginMessage, GetLoginErrorMessage(www), false);
            }
        }

        isLoggingIn = false;
    }

    // 가입에 성공하면 로그인 창으로 바꾸고, 방금 만든 아이디를 채운 뒤 비밀번호 칸으로 이동
    IEnumerator RegisterAction()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", regID.text);
        form.AddField("password", regPW.text);
        form.AddField("nickname", NormalizeNickname(regNick.text));

        using (UnityWebRequest www = UnityWebRequest.Post(ApiBaseUrl + "/api/users/register", form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                Debug.Log("<color=green>회원가입 성공!</color>");
                string registeredId = regID.text;
                OpenLogin();

                loginID.text = registeredId;
                loginPW.Select();
                loginPW.ActivateInputField();
            } else {
                Debug.LogError("회원가입 실패: " + www.error);
                ShowRegisterError(www);
            }
        }
    }

    // 서버가 "true"를 돌려주면 이미 있는 아이디
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
                    ShowRegMessage(regIdMessage, "이미 사용중인 아이디입니다.", false);
                } else {
                    isUsernameChecked = true;
                    checkedUsername = regID.text;
                    ShowRegMessage(regIdMessage, "사용 가능한 아이디입니다.", true);
                }
            } else {
                Debug.LogError("아이디 중복 확인 실패: " + www.error);
                ShowRegMessage(regIdMessage, "중복 확인에 실패했습니다.", false);
            }
        }
    }

    // 닉네임은 앞뒤 공백을 뺀 값으로 확인 (가입할 때와 같은 기준)
    IEnumerator CheckNicknameDuplicateAction()
    {
        string nicknameToCheck = NormalizeNickname(regNick.text);
        string url = ApiBaseUrl + "/api/users/check-nickname?nickname=" + UnityWebRequest.EscapeURL(nicknameToCheck);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) {
                bool isDuplicate = www.downloadHandler.text.Trim().ToLower() == "true";

                if (isDuplicate) {
                    isNicknameChecked = false;
                    ShowRegMessage(regNickMessage, "이미 사용중인 닉네임입니다.", false);
                } else {
                    isNicknameChecked = true;
                    checkedNickname = nicknameToCheck;
                    ShowRegMessage(regNickMessage, "사용 가능한 닉네임입니다.", true);
                }
            } else {
                Debug.LogError("닉네임 중복 확인 실패: " + www.error);
                ShowRegMessage(regNickMessage, "중복 확인에 실패했습니다.", false);
            }
        }
    }

    // 닉네임 비교 기준: 앞뒤 공백만 제거하고 대소문자는 구분 (서버도 같은 기준)
    private static string NormalizeNickname(string raw)
    {
        return raw == null ? "" : raw.Trim();
    }

    // 가입 실패 안내. 중복 확인 뒤 가입 직전에 다른 사람이 먼저 가입하면 서버가 409와 함께 어떤 항목이 겹쳤는지 알려준다
    private void ShowRegisterError(UnityWebRequest www)
    {
        if (www.result == UnityWebRequest.Result.ConnectionError) {
            ShowRegMessage(regSubmitMessage, "서버에 연결할 수 없습니다.", false);
            return;
        }

        if (www.responseCode == 409) {
            string body = www.downloadHandler != null ? www.downloadHandler.text : "";
            if (body.Contains("NICKNAME_DUPLICATE")) {
                isNicknameChecked = false;
                ShowRegMessage(regNickMessage, "이미 사용중인 닉네임입니다.", false);
                return;
            }
            if (body.Contains("USERNAME_DUPLICATE")) {
                isUsernameChecked = false;
                ShowRegMessage(regIdMessage, "이미 사용중인 아이디입니다.", false);
                return;
            }
        }

        ShowRegMessage(regSubmitMessage, "회원가입에 실패했습니다.", false);
    }

    // 가입 전 입력 검사. 문제가 있는 칸 옆에 안내를 띄우고 false
    private bool ValidateRegisterInput()
    {
        if (string.IsNullOrWhiteSpace(regID.text) || string.IsNullOrWhiteSpace(regPW.text) || string.IsNullOrWhiteSpace(regNick.text)) {
            ShowRegMessage(regSubmitMessage, "모든 항목을 입력해주세요.", false);
            return false;
        }

        if (!isUsernameChecked || checkedUsername != regID.text) {
            ShowRegMessage(regIdMessage, "아이디 중복 확인을 해주세요.", false);
            return false;
        }

        if (!isNicknameChecked || checkedNickname != NormalizeNickname(regNick.text)) {
            ShowRegMessage(regNickMessage, "닉네임 중복 확인을 해주세요.", false);
            return false;
        }

        if (regPWConfirm != null && regPW.text != regPWConfirm.text) {
            ShowRegMessage(regPwMessage, "비밀번호가 일치하지 않습니다.", false);
            return false;
        }

        return true;
    }

    // 안내 문구 표시 (성공은 초록, 실패는 빨강)
    private void ShowRegMessage(TMP_Text target, string message, bool success)
    {
        if (target == null) return;
        target.text = message;
        target.color = success ? new Color(0.35f, 0.85f, 0.35f) : new Color(0.9f, 0.3f, 0.25f);
    }

    // 서버 연결 실패와 아이디/비밀번호 오류를 구분해서 안내
    private string GetLoginErrorMessage(UnityWebRequest www)
    {
        if (www.result == UnityWebRequest.Result.ConnectionError)
            return "서버에 연결할 수 없습니다.";
        return "아이디 또는 비밀번호가 일치하지 않습니다.";
    }

    private void ClearRegMessages()
    {
        ShowRegMessage(loginMessage, "", true);
        ShowRegMessage(regIdMessage, "", true);
        ShowRegMessage(regPwMessage, "", true);
        ShowRegMessage(regNickMessage, "", true);
        ShowRegMessage(regSubmitMessage, "", true);
    }
}