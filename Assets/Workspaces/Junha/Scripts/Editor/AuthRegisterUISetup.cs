using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 회원가입 패널에 아이디 중복확인 버튼 / 비밀번호 확인란 / 비밀번호 보기 버튼을 자동으로 배치하는 1회성 에디터 툴.
// 사용법: AuthScene을 연 상태에서 Tools > Auth > 회원가입 UI 자동 배치 실행.
public static class AuthRegisterUISetup
{
    private const string ButtonPrefabPath =
        "Assets/Workspaces/Junha/Alebardium/Bloodlines UI/Prefabs/Button/Button 3 (Gray).prefab";

    private const string FontGuid = "87b7a5c12adc07c4fa07bec4ec66bf03"; // 입력창에서 쓰는 폰트와 통일

    [MenuItem("Tools/Auth/회원가입 UI 자동 배치")]
    private static void Setup()
    {
        AuthManager authManager = Object.FindFirstObjectByType<AuthManager>();
        if (authManager == null)
        {
            Debug.LogError("[AuthRegisterUISetup] 씬에서 AuthManager를 찾을 수 없습니다. AuthScene을 열어주세요.");
            return;
        }

        if (authManager.regPWConfirm != null)
        {
            Debug.LogWarning("[AuthRegisterUISetup] 이미 배치되어 있는 것 같습니다 (regPWConfirm이 이미 연결됨). 다시 실행하려면 기존 오브젝트를 정리하세요.");
            return;
        }

        if (authManager.regID == null || authManager.regPW == null || authManager.regNick == null || authManager.registerPanel == null)
        {
            Debug.LogError("[AuthRegisterUISetup] AuthManager의 regID/regPW/regNick/registerPanel 참조가 비어 있습니다.");
            return;
        }

        Transform panel = authManager.registerPanel.transform;
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null)
        {
            Debug.LogError($"[AuthRegisterUISetup] 버튼 프리팹을 찾을 수 없습니다: {ButtonPrefabPath}");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));

        Undo.SetCurrentGroupName("회원가입 UI 자동 배치");
        int undoGroup = Undo.GetCurrentGroup();

        // -----------------------------
        // 1. 아이디 입력창 + 중복확인 버튼
        // -----------------------------
        SetRect(authManager.regID.GetComponent<RectTransform>(), new Vector2(290, 60), new Vector2(-55, 110));

        Button checkDuplicateButton = CreateButton(buttonPrefab, panel, "CheckDuplicate_Btn",
            new Vector2(140, 50), new Vector2(165, 110), "중복확인");
        UnityEventTools.AddPersistentListener(checkDuplicateButton.onClick, authManager.CheckUsernameDuplicateClick);

        // -----------------------------
        // 2. 비밀번호 입력창 (마스킹) + 보기 버튼
        // -----------------------------
        Undo.RecordObject(authManager.regPW, "비밀번호 마스킹 설정");
        authManager.regPW.contentType = TMP_InputField.ContentType.Password;
        authManager.regPW.ForceLabelUpdate();
        SetRect(authManager.regPW.GetComponent<RectTransform>(), new Vector2(290, 60), new Vector2(-55, 20));

        Button toggleButton = CreateButton(buttonPrefab, panel, "TogglePassword_Btn",
            new Vector2(140, 50), new Vector2(165, 20), "보기");
        UnityEventTools.AddPersistentListener(toggleButton.onClick, authManager.TogglePasswordVisibilityClick);

        // -----------------------------
        // 3. 비밀번호 확인 입력창 (regPW를 복제)
        // -----------------------------
        GameObject regPWConfirmGO = Object.Instantiate(authManager.regPW.gameObject, panel);
        Undo.RegisterCreatedObjectUndo(regPWConfirmGO, "비밀번호 확인 입력창 생성");
        regPWConfirmGO.name = "RegPWConfirm_Input";

        TMP_InputField regPWConfirm = regPWConfirmGO.GetComponent<TMP_InputField>();
        regPWConfirm.text = "";
        regPWConfirm.contentType = TMP_InputField.ContentType.Password;
        if (regPWConfirm.placeholder is TMP_Text placeholderText)
        {
            placeholderText.text = "비밀번호 확인";
        }
        regPWConfirm.ForceLabelUpdate();
        SetRect(regPWConfirmGO.GetComponent<RectTransform>(), new Vector2(400, 60), new Vector2(0, -70));

        // -----------------------------
        // 4. 닉네임 입력창 아래로 이동
        // -----------------------------
        SetRect(authManager.regNick.GetComponent<RectTransform>(), new Vector2(400, 60), new Vector2(0, -160));

        // -----------------------------
        // 5. 안내 메시지 텍스트
        // -----------------------------
        GameObject messageGO = new GameObject("RegMessage_Text", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(messageGO, "회원가입 메시지 텍스트 생성");
        messageGO.transform.SetParent(panel, false);
        messageGO.layer = panel.gameObject.layer;

        TextMeshProUGUI messageText = messageGO.AddComponent<TextMeshProUGUI>();
        messageText.font = font;
        messageText.fontSize = 26;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.text = "";
        SetRect(messageGO.GetComponent<RectTransform>(), new Vector2(500, 40), new Vector2(0, -215));

        // -----------------------------
        // 6. 가입하기 / 돌아가기 버튼 아래로 재배치 (새 필드 자리 확보)
        // -----------------------------
        RectTransform registerBtnRect = panel.Find("Register_Btn") as RectTransform;
        if (registerBtnRect != null)
        {
            SetRect(registerBtnRect, registerBtnRect.sizeDelta, new Vector2(0, -270));
        }

        RectTransform backToLoginBtnRect = panel.Find("BackToLogin_Btn") as RectTransform;
        if (backToLoginBtnRect != null)
        {
            SetRect(backToLoginBtnRect, backToLoginBtnRect.sizeDelta, new Vector2(0, -360));
        }

        // -----------------------------
        // 7. AuthManager 참조 연결
        // -----------------------------
        Undo.RecordObject(authManager, "AuthManager 참조 연결");
        authManager.regPWConfirm = regPWConfirm;
        authManager.regMessage = messageText;
        authManager.togglePasswordButtonLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>();

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.SetDirty(authManager);
        EditorSceneManager.MarkSceneDirty(authManager.gameObject.scene);

        Debug.Log("<color=green>[AuthRegisterUISetup] 회원가입 UI 배치 완료. 위치를 확인한 뒤 씬을 저장하세요 (Ctrl+S).</color>");
    }

    private static Button CreateButton(GameObject prefab, Transform parent, string name, Vector2 size, Vector2 anchoredPosition, string label)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "버튼 생성: " + name);
        instance.name = name;

        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        SetRect(rect, size, anchoredPosition);

        TextMeshProUGUI text = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = label;
            text.fontSize = 22;
        }

        return instance.GetComponent<Button>();
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
    {
        Undo.RecordObject(rect, "레이아웃 조정");
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }
}
