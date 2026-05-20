using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void StartGame()
    {
        // 로비에서 클릭하면 이동할 씬 이름이 "GameScene"인 거 확인!
        // 만약 대소문자가 다르거나 이름이 다르면 실제 씬 이름으로 바꿔야 해.
        SceneManager.LoadScene("GameScene");
    }
}