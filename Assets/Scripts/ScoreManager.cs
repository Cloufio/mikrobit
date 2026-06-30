// ScoreManager.cs
using UnityEngine;
using UnityEngine.UI; // Ensure this is present for legacy UI Text

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // --- Score Variables (Existing) ---
    public Text scoreTextElement;
    public int currentScore = 0;

    // --- Timer Variables (Existing) ---
    public Text timerTextElement;
    public float timeRemaining = 60f;
    public bool timerIsRunning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentScore = 0;
        UpdateScoreDisplay();

        timerIsRunning = true;
        DisplayTime(timeRemaining); // Initialize timer display
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                Debug.Log("Time has run out!");
                timeRemaining = 0;
                timerIsRunning = false;
                DisplayTime(timeRemaining); // Ensure display shows 00:00
                // TimeUpActions(); // Call your game over logic
            }
        }
    }

    public void AddScore(int pointsToAdd)
    {
        currentScore += pointsToAdd;
        UpdateScoreDisplay();
    }

    void UpdateScoreDisplay()
    {
        if (scoreTextElement != null)
        {
            scoreTextElement.text = "Score : " + currentScore;
        }
        else
        {
            Debug.LogError("Score Text Element is not assigned in the ScoreManager!");
        }
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    // --- Timer Display Method (MODIFIED for Seconds:Milliseconds) ---
    void DisplayTime(float timeToDisplay)
    {
        if (timerTextElement == null)
        {
            Debug.LogError("Timer Text Element is not assigned in the ScoreManager!");
            return;
        }

        if (timeToDisplay < 0)
        {
            timeToDisplay = 0;
        }

        // Calculate total seconds and hundredths of a second (milliseconds)
        float totalSeconds = Mathf.FloorToInt(timeToDisplay);
        // To get the hundredths of a second part (00-99):
        float hundredths = Mathf.FloorToInt((timeToDisplay * 100f) % 100f);

        // Update the timerTextElement with the new format "SS:MS"
        // For example, 5.23 seconds will be "05:23"
        // 65.78 seconds will be "65:78"
        timerTextElement.text = string.Format("Time : {0:00}:{1:00}", totalSeconds, hundredths);
    }

    // void TimeUpActions()
    // {
    //     Debug.Log("GAME OVER - Time's Up!");
    //     // Time.timeScale = 0;
    // }
}
