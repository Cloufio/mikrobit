using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void playGame()
    {
        SceneManager.LoadScene(1);
    }

    public void SkipStory()
    {
        SceneManager.LoadScene(2);
    }

    public void ExitGame()
    {
        Debug.Log("Exit button pressed!");
        Application.Quit();
    }
}