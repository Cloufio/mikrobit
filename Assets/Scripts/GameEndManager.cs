using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameEndManager : MonoBehaviour
{
    [Header("Scene Transitions")]
    [Tooltip("Scene shown when the timer expires or the player runs out of health.")]
    public string finalScoreSceneName = "FinalScoreScene";

    [Header("Fade Settings")]
    [Tooltip("The UI Image to use for fading in the Inspector.")]
    public Image fadePanel;
    [Tooltip("How long the fade-in at the start should take in seconds.")]
    public float fadeInDuration = 1.5f;  // NEW: Specific duration for fade-in
    [Tooltip("How long the fade-out at the end should take in seconds.")]
    public float fadeOutDuration = 2f; // RENAMED: from fadeDuration for clarity

    private bool conditionsHaveBeenMet = false;
    private ScoreManager scoreManagerInstance;
    private PlayerHealth playerHealth;

    void Start()
    {
        scoreManagerInstance = ScoreManager.Instance;
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (scoreManagerInstance == null)
        {
            Debug.LogError("GameEndManager: ScoreManager.Instance not found!");
            enabled = false;
            return;
        }

        if (fadePanel == null)
        {
            Debug.LogError("GameEndManager: Fade Panel has not been assigned in the Inspector!");
            enabled = false;
            return;
        }

        // CHANGED: Instead of making the panel transparent, we start the fade-in coroutine.
        StartCoroutine(FadeInScene());
    }

    // NEW: This entire coroutine handles the fade-in at the beginning of the scene.
    IEnumerator FadeInScene()
    {
        // Ensure the panel is active and fully opaque (black) at the very start.
        fadePanel.gameObject.SetActive(true);
        fadePanel.color = new Color(fadePanel.color.r, fadePanel.color.g, fadePanel.color.b, 1f);

        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            // Animate the alpha value from 1 (opaque) down to 0 (transparent).
            float newAlpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeInDuration);
            fadePanel.color = new Color(fadePanel.color.r, fadePanel.color.g, fadePanel.color.b, newAlpha);
            yield return null;
        }

        // Ensure it's fully transparent at the end.
        fadePanel.color = new Color(fadePanel.color.r, fadePanel.color.g, fadePanel.color.b, 0f);
        // Good practice: Deactivate the panel so it doesn't block clicks on other UI elements.
        fadePanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (conditionsHaveBeenMet || scoreManagerInstance == null)
        {
            return;
        }

        int currentScore = scoreManagerInstance.currentScore;
        bool timeIsOver = !scoreManagerInstance.timerIsRunning && scoreManagerInstance.timeRemaining <= 0;
        bool healthIsDepleted = playerHealth != null && playerHealth.currentHealth <= 0;

        if (timeIsOver || healthIsDepleted)
        {
            conditionsHaveBeenMet = true;
            SaveRunScore(currentScore);
            string endReason = healthIsDepleted ? "Health depleted" : "Time has run out";
            Debug.Log($"{endReason}. Loading '{finalScoreSceneName}' with score {currentScore}.");

            // Re-activate the panel before starting the fade-out.
            fadePanel.gameObject.SetActive(true);
            StartCoroutine(PerformFadeAndLoadScene(finalScoreSceneName));
        }
    }

    private static void SaveRunScore(int currentScore)
    {
        int bestScore = Mathf.Max(PlayerPrefs.GetInt("Microbit_BestScore", 0), currentScore);
        PlayerPrefs.SetInt("Microbit_LastScore", currentScore);
        PlayerPrefs.SetInt("Microbit_BestScore", bestScore);
        PlayerPrefs.Save();
    }

    IEnumerator PerformFadeAndLoadScene(string targetSceneName)
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }

        float elapsedTime = 0f;
        Color panelColor = fadePanel.color;

        // Use the new fadeOutDuration variable here.
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Clamp01(elapsedTime / fadeOutDuration);
            fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, newAlpha);
            yield return null;
        }

        fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, 1f);
        SceneManager.LoadScene(targetSceneName);
    }
}
