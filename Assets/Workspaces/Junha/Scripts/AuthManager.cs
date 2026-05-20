using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

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
            } else {
                Debug.LogError("로그인 실패: " + www.error);
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
                OpenLogin(); // 가입 성공 시 로그인 화면으로 전환
            } else {
                Debug.LogError("회원가입 실패: " + www.error);
            }
        }
    }
}