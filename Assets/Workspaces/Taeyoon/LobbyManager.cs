using UnityEngine;
using UnityEngine.UI; // 이미지 제어용
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    public string nextSceneName = "GameScene";
    public Image fadeImage; // 아까 만든 FadeImage 연결용
    private bool isTransitioning = false;

    void Update()
    {
        // 클릭하면 스르륵 연출 시작
        if (Input.GetMouseButtonDown(0) && !isTransitioning)
        {
            StartCoroutine(FadeOutAndLoad());
        }
    }

    IEnumerator FadeOutAndLoad()
    {
        isTransitioning = true;
        float duration = 1.0f; // 1초 동안 스르륵
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 검은 이미지의 투명도(A)를 0에서 1로 서서히 올림
            fadeImage.color = new Color(0, 0, 0, timer / duration);
            yield return null;
        }

        // 연출이 끝나면 씬 전환
        SceneManager.LoadScene(nextSceneName);
    }
}