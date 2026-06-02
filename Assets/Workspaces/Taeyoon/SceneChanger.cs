using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void StartGame()
    {
        // �κ񿡼� Ŭ���ϸ� �̵��� �� �̸��� "GameScene"�� �� Ȯ��!
        // ���� ��ҹ��ڰ� �ٸ��ų� �̸��� �ٸ��� ���� �� �̸����� �ٲ�� ��.
        SceneManager.LoadScene("GameScene");
    }
}