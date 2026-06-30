using UnityEngine;
using UnityEngine.UI;         // Required for UI Image
using UnityEngine.SceneManagement; // Required for scene management
using System.Collections;     // Required for Coroutines

public class BadEndingSceneController : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("The UI Image to use for fading. It should cover the screen.")]
    public Image fadePanel;
    [Tooltip("How long the fade-in effect should take in seconds.")]
    public float fadeInDuration = 1.5f;
    [Tooltip("How long the fade-out effect should take in seconds.")]
    public float fadeOutDuration = 1.5f;
    [Tooltip("How long to wait (in seconds) after fade-in before starting fade-out.")]
    public float waitDuration = 2.0f;

    [Header("Scene Transition")]
    [Tooltip("The build index of the scene to load after fade-out (e.g., 0 for Main Menu).")]
    public int sceneIndexToLoad = 0;

    void Start()
    {
        if (fadePanel == null)
        {
            Debug.LogError("BadEndingSceneController: Fade Panel is not assigned in the Inspector!");
            // Optionally, try to find it if not assigned, or disable the script
            // For simplicity, we'll just log an error. The scene won't fade correctly.
            enabled = false;
            return;
        }

        // Ensure Time.timeScale is 1 when this scene starts, in case it was set to 0 previously.
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Debug.Log("BadEndingSceneController: Time.timeScale was not 1, resetting to 1.");
        }

        // Start the sequence
        StartCoroutine(SceneSequenceCoroutine());
    }

    IEnumerator SceneSequenceCoroutine()
    {
        // 1. Start with the panel fully opaque (if it's not already, e.g., from a previous scene's fade-out)
        //    and then fade it in (alpha from 1 to 0).
        fadePanel.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration)); // Fade In

        // 2. Wait for the specified duration
        Debug.Log($"BadEndingSceneController: Fade-in complete. Waiting for {waitDuration} seconds.");
        yield return new WaitForSeconds(waitDuration);

        // 3. Fade out (alpha from 0 to 1)
        Debug.Log("BadEndingSceneController: Wait complete. Starting fade-out.");
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration)); // Fade Out

        // 4. Load the next scene
        Debug.Log($"BadEndingSceneController: Fade-out complete. Loading scene with build index {sceneIndexToLoad}.");
        SceneManager.LoadScene(sceneIndexToLoad);
    }

    IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color panelColor = fadePanel.color; // Get the base color (e.g., black)

        // Ensure the panel starts at the correct alpha
        fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, startAlpha);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime; // Use unscaled time if you want fade during Time.timeScale = 0, but usually not needed here
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, newAlpha);
            yield return null; // Wait for the next frame
        }

        // Ensure the panel is exactly at the endAlpha
        fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, endAlpha);
    }
}
