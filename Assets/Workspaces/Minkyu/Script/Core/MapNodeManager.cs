using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNodeManager : MonoBehaviour
{
    public string battleSceneName = "JunhaTest";

    public void GoToBattle()
    {
        SceneManager.LoadScene(battleSceneName);
    }
}