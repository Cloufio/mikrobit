using System;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps one LootLocker guest session alive between scenes and owns the menu
/// overlays for choosing a display name and viewing the global leaderboard.
/// </summary>
[DisallowMultipleComponent]
public class LeaderboardManager : MonoBehaviour
{
    private const string GuestIdentifierKey = "Microbit.LootLocker.GuestIdentifier";
    private const string PlayerNameKey = "Microbit.PlayerName";
    private const string MemberIdKey = "Microbit.LootLocker.MemberId";
    private const string PendingScoreKey = "Microbit.LootLocker.PendingScore";

    public static LeaderboardManager Instance { get; private set; }

    [Header("LootLocker")]
    [Tooltip("LootLocker leaderboard key. The current Global_Leaderboard uses global_leaderboard.")]
    [SerializeField] private string leaderboardKey = "global_leaderboard";
    [SerializeField, Min(1)] private int leaderboardEntryCount = 10;
    [SerializeField, Min(1f)] private float requestTimeoutSeconds = 12f;

    [Header("Overlay Styling")]
    [SerializeField] private Color dimColor = new(0.015f, 0.035f, 0.07f, 0.82f);
    [SerializeField] private Color panelColor = new(0.035f, 0.11f, 0.20f, 0.98f);
    [SerializeField] private Color accentColor = new(1f, 0.66f, 0.22f, 1f);
    [SerializeField] private Color bodyColor = new(0.94f, 0.98f, 1f, 1f);

    private readonly List<Action<bool>> waitingForSession = new();
    private bool sessionReady;
    private bool sessionStarting;
    private string memberId;
    private string lastSessionError;
    private TMP_InputField usernameInput;
    private GameObject activeOverlay;

    public static LeaderboardManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LeaderboardManager existing = FindObjectOfType<LeaderboardManager>();
        if (existing != null)
        {
            return existing;
        }

        GameObject managerObject = new("Leaderboard Manager");
        return managerObject.AddComponent<LeaderboardManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureGuestIdentifier();
        RetryPendingScore();
    }

    public void PromptForPlayerName(UnityAction onConfirmed)
    {
        CloseOverlay();

        GameObject panel = CreateOverlay("Player Name", new Vector2(680f, 390f));
        AddText(panel.transform, "PLAYER NAME", 42f, accentColor, new Vector2(0f, 106f), new Vector2(560f, 58f));
        AddText(panel.transform, "Choose the name shown on the global leaderboard.", 23f, bodyColor, new Vector2(0f, 52f), new Vector2(560f, 44f));

        usernameInput = AddInput(panel.transform, new Vector2(0f, -12f), new Vector2(520f, 76f));
        usernameInput.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        usernameInput.characterLimit = 16;
        usernameInput.onValidateInput += ValidatePlayerName;

        Button startButton = AddButton(panel.transform, "START CLEANUP", new Vector2(0f, -116f), new Vector2(310f, 64f));
        startButton.onClick.AddListener(() => ConfirmPlayerName(onConfirmed));
        usernameInput.onSubmit.AddListener(_ => ConfirmPlayerName(onConfirmed));

        usernameInput.Select();
        usernameInput.ActivateInputField();
    }

    public void ShowGlobalLeaderboard()
    {
        CloseOverlay();
        SceneManager.LoadScene("LeaderboardScene");
    }

    /// <summary>
    /// Reads the configured LootLocker leaderboard for the dedicated
    /// LeaderboardScene. The callback is guaranteed to finish, even when a
    /// network request never returns.
    /// </summary>
    public void RequestLeaderboard(Action<bool, LootLockerLeaderboardMember[], string> onComplete)
    {
        RetryPendingScore();
        StartCoroutine(RequestLeaderboardRoutine(onComplete));
    }

    public void SubmitCompletedScore(int score)
    {
        if (score < 0)
        {
            return;
        }

        // Keep the score until LootLocker explicitly confirms it. This lets a
        // run survive a scene transition, a slow guest session, or a brief
        // connection failure without silently disappearing from the board.
        PlayerPrefs.SetInt(PendingScoreKey, score);
        PlayerPrefs.Save();
        SubmitPendingScore();
    }

    private void RetryPendingScore()
    {
        if (PlayerPrefs.HasKey(PendingScoreKey))
        {
            SubmitPendingScore();
        }
    }

    private void SubmitPendingScore()
    {
        if (!PlayerPrefs.HasKey(PendingScoreKey))
        {
            return;
        }

        int score = Mathf.Max(0, PlayerPrefs.GetInt(PendingScoreKey));
        EnsureSession(ready =>
        {
            if (!ready)
            {
                Debug.LogWarning("LootLocker score is queued until the guest session can start.");
                return;
            }

            string displayName = GetDisplayName();
            // This is a Player leaderboard. LootLocker uses the active guest
            // session automatically, so member_id must remain blank.
            LootLockerSDKManager.SubmitScore(string.Empty, score, leaderboardKey, displayName, response =>
            {
                if (!response.success)
                {
                    Debug.LogWarning($"LootLocker could not submit score: {GetError(response)}");
                    return;
                }

                PlayerPrefs.DeleteKey(PendingScoreKey);
                PlayerPrefs.Save();
                Debug.Log($"LootLocker submitted score {response.score} to '{leaderboardKey}' at rank {response.rank}.");
            });
        });
    }

    private void ConfirmPlayerName(UnityAction onConfirmed)
    {
        string name = SanitizeName(usernameInput != null ? usernameInput.text : string.Empty);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        PlayerPrefs.SetString(PlayerNameKey, name);
        PlayerPrefs.Save();
        CloseOverlay();

        // Starting the game must not depend on the network responding. The
        // session can finish in the background before the final score is sent.
        EnsureSession(null);
        onConfirmed?.Invoke();
    }

    private System.Collections.IEnumerator RequestLeaderboardRoutine(Action<bool, LootLockerLeaderboardMember[], string> onComplete)
    {
        if (!HasLootLockerApiKey())
        {
            onComplete?.Invoke(false, null, "LootLocker API key is missing. Open Edit > Project Settings > LootLocker SDK and paste this game's Game API Key.");
            yield break;
        }

        bool sessionFinished = false;
        bool sessionSucceeded = false;
        EnsureSession(ready =>
        {
            sessionSucceeded = ready;
            sessionFinished = true;
        });

        float timeoutAt = Time.realtimeSinceStartup + requestTimeoutSeconds;
        while (!sessionFinished && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }

        if (!sessionFinished)
        {
            onComplete?.Invoke(false, null, "Could not reach LootLocker. Check your connection and LootLocker API key.");
            yield break;
        }

        if (!sessionSucceeded)
        {
            string message = string.IsNullOrEmpty(lastSessionError)
                ? "LootLocker guest session failed. Check the API key and Guest platform in LootLocker."
                : $"LootLocker guest session failed: {lastSessionError}";
            onComplete?.Invoke(false, null, message);
            yield break;
        }

        bool scoresFinished = false;
        LootLockerGetScoreListResponse scoreResponse = null;
        LootLockerSDKManager.GetScoreList(leaderboardKey, leaderboardEntryCount, response =>
        {
            scoreResponse = response;
            scoresFinished = true;
        });

        timeoutAt = Time.realtimeSinceStartup + requestTimeoutSeconds;
        while (!scoresFinished && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }

        if (!scoresFinished)
        {
            onComplete?.Invoke(false, null, "The leaderboard request timed out. Check your connection and leaderboard key.");
            yield break;
        }

        if (scoreResponse == null || !scoreResponse.success)
        {
            string error = scoreResponse == null ? "No response from LootLocker." : GetError(scoreResponse);
            onComplete?.Invoke(false, null, $"Could not load scores: {error}");
            yield break;
        }

        onComplete?.Invoke(true, scoreResponse.items ?? Array.Empty<LootLockerLeaderboardMember>(), string.Empty);
    }

    private void EnsureSession(Action<bool> onComplete)
    {
        if (sessionReady)
        {
            onComplete?.Invoke(true);
            return;
        }

        waitingForSession.Add(onComplete);
        if (sessionStarting)
        {
            return;
        }

        sessionStarting = true;
        LootLockerSDKManager.StartGuestSession(EnsureGuestIdentifier(), response =>
        {
            sessionStarting = false;
            sessionReady = response.success;
            memberId = response.success ? response.player_id.ToString() : string.Empty;
            lastSessionError = sessionReady ? string.Empty : GetError(response);

            if (sessionReady)
            {
                PlayerPrefs.SetString(MemberIdKey, memberId);
                PlayerPrefs.Save();
            }

            if (!sessionReady)
            {
                Debug.LogWarning($"LootLocker guest session failed: {GetError(response)}");
            }

            Action<bool>[] callbacks = waitingForSession.ToArray();
            waitingForSession.Clear();
            foreach (Action<bool> callback in callbacks)
            {
                callback?.Invoke(sessionReady);
            }
        });
    }

    private static bool HasLootLockerApiKey()
    {
        LootLockerConfig config = LootLockerConfig.Get();
        return config != null && !string.IsNullOrWhiteSpace(config.apiKey);
    }

    private string EnsureGuestIdentifier()
    {
        string identifier = PlayerPrefs.GetString(GuestIdentifierKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            return identifier;
        }

        identifier = "microbit-" + Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(GuestIdentifierKey, identifier);
        PlayerPrefs.Save();
        return identifier;
    }

    private GameObject CreateOverlay(string name, Vector2 panelSize)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Runtime UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject overlay = CreateUiObject(name, canvas.transform);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect);
        Image dimmer = overlay.AddComponent<Image>();
        dimmer.color = dimColor;

        GameObject panel = CreateUiObject("Panel", overlay.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = panelSize;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        activeOverlay = overlay;
        return panel;
    }

    private TMP_Text AddText(Transform parent, string value, float fontSize, Color color, Vector2 position, Vector2 size)
    {
        GameObject textObject = CreateUiObject("Text", parent);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private TMP_InputField AddInput(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject inputObject = CreateUiObject("Username Input", parent);
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        inputObject.AddComponent<Image>().color = new Color(0.08f, 0.19f, 0.30f, 1f);

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        TMP_Text text = AddText(inputObject.transform, string.Empty, 31f, bodyColor, Vector2.zero, size - new Vector2(28f, 0f));
        text.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_Text placeholder = AddText(inputObject.transform, "ENTER YOUR NAME", 26f, new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.46f), Vector2.zero, size - new Vector2(28f, 0f));
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Alphanumeric;
        return input;
    }

    private Button AddButton(Transform parent, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(label + " Button", parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = buttonObject.AddComponent<Image>();
        image.color = accentColor;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.75f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        button.colors = colors;

        TMP_Text text = AddText(buttonObject.transform, label, 26f, new Color(0.06f, 0.09f, 0.14f, 1f), Vector2.zero, size);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void CloseOverlay()
    {
        if (activeOverlay != null)
        {
            Destroy(activeOverlay);
            activeOverlay = null;
            usernameInput = null;
        }
    }

    private static char ValidatePlayerName(string text, int charIndex, char addedChar)
    {
        return char.IsLetterOrDigit(addedChar) || addedChar == ' ' || addedChar == '-' || addedChar == '_' ? addedChar : '\0';
    }

    private static string SanitizeName(string rawName)
    {
        return string.IsNullOrWhiteSpace(rawName) ? string.Empty : rawName.Trim();
    }

    private static string GetError(LootLockerResponse response)
    {
        return response?.errorData?.message ?? "Unknown error";
    }

    private static string GetDisplayName()
    {
        string savedName = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        return string.IsNullOrWhiteSpace(savedName) ? "OCEAN CLEANER" : savedName;
    }
}
