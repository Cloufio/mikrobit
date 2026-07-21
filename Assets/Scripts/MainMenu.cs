using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void playGame()
    {
        SceneManager.LoadScene("IntroScene");
    }

    public void SkipStory()
    {
        SceneManager.LoadScene("MainScene2");
    }

    public void ExitGame()
    {
        Debug.Log("Exit button pressed!");
        Application.Quit();
    }
}
