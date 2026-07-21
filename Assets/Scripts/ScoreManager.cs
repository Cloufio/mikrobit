using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public Text scoreTextElement;
    public int currentScore = 0;

    public Text timerTextElement;
    public Image timerIconElement;
    public Sprite[] timerGreenFrames;
    public Sprite[] timerYellowFrames;
    public Sprite[] timerRedFrames;
    public float timeRemaining = 60f;
    public bool timerIsRunning = false;
    private float startingTime;

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
        startingTime = Mathf.Max(timeRemaining, 0.01f);
        currentScore = 0;
        UpdateScoreDisplay();

        // The countdown begins only after the player boards the boat.
        timerIsRunning = false;
        DisplayTime(timeRemaining);
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
                DisplayTime(timeRemaining);
            }
        }
    }

    public void AddScore(int pointsToAdd)
    {
        currentScore += pointsToAdd;
        UpdateScoreDisplay();
    }

    public void StartTimer()
    {
        if (timeRemaining <= 0f)
        {
            return;
        }

        timerIsRunning = true;
        DisplayTime(timeRemaining);
    }

    void UpdateScoreDisplay()
    {
        if (scoreTextElement != null)
        {
            // Instead of adding the word "Score\n", we just convert the number to a string
            scoreTextElement.text = currentScore.ToString();
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
        float hundredths = Mathf.FloorToInt((timeToDisplay * 100f) % 100f);

        timerTextElement.text = string.Format("{0:00}:{1:00}", totalSeconds, hundredths);
        UpdateTimerIcon(timeToDisplay);
    }

    void UpdateTimerIcon(float timeToDisplay)
    {
        if (timerIconElement == null)
        {
            return;
        }

        float remainingPercent = Mathf.Clamp01(timeToDisplay / startingTime);
        Sprite[] frames = GetTimerFrameSet(remainingPercent);
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        int frameIndex = Mathf.RoundToInt((1f - remainingPercent) * (frames.Length - 1));
        timerIconElement.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
    }

    Sprite[] GetTimerFrameSet(float remainingPercent)
    {
        if (remainingPercent > 0.66f)
        {
            return timerGreenFrames;
        }

        if (remainingPercent > 0.33f)
        {
            return timerYellowFrames;
        }

        return timerRedFrames;
    }
}
