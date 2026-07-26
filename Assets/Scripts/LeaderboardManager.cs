using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps one LootLocker guest session alive between scenes and owns the player
/// identity flow. Player names are set on the LootLocker profile so the same
/// name is used both when scores are submitted and when the board is read.
/// </summary>
[DisallowMultipleComponent]
public class LeaderboardManager : MonoBehaviour
{
    private const string GuestIdentifierKey = "Microbit.LootLocker.GuestIdentifier";
    private const string PlayerNameKey = "Microbit.PlayerName";
    private const string PlayerNameNormalizedKey = "Microbit.PlayerNameNormalized";
    private const string MemberIdKey = "Microbit.LootLocker.MemberId";
    private const string PendingScoreKey = "Microbit.LootLocker.PendingScore";

    public static LeaderboardManager Instance { get; private set; }
    public event Action<string> PlayerNameChanged;

    public string CurrentPlayerName => GetDisplayName();

    [Header("LootLocker")]
    [Tooltip("LootLocker leaderboard key. The current Global_Leaderboard uses global_leaderboard.")]
    [SerializeField] private string leaderboardKey = "global_leaderboard";
    [SerializeField, Min(1)] private int leaderboardEntryCount = 10;
    [SerializeField, Min(10)] private int usernameValidationEntryCount = 200;
    [SerializeField, Min(1f)] private float requestTimeoutSeconds = 12f;

    [Header("Name Prompt Styling")]
    [Tooltip("Optional. Leave empty to reuse the first TMP font in the active scene, normally BoldPixels.")]
    [SerializeField] private TMP_FontAsset boldPixelsFont;
    [SerializeField] private Color dimColor = new(0.015f, 0.035f, 0.07f, 0.82f);
    [SerializeField] private Color panelColor = new(0.035f, 0.11f, 0.20f, 0.98f);
    [SerializeField] private Color inputColor = new(0.08f, 0.19f, 0.30f, 1f);
    [SerializeField] private Color accentColor = new(1f, 0.66f, 0.22f, 1f);
    [SerializeField] private Color bodyColor = new(0.94f, 0.98f, 1f, 1f);
    [SerializeField] private Color warningColor = new(1f, 0.48f, 0.35f, 1f);
    [SerializeField] private Color placeholderColor = new(0.94f, 0.98f, 1f, 0.46f);
    [SerializeField] private Color caretColor = new(0.94f, 0.98f, 1f, 1f);
    [SerializeField, Min(1f)] private float usernameCaretWidth = 3f;

    [Header("Name Prompt Layout")]
    [SerializeField] private Vector2 usernamePromptSize = new(720f, 320f);
    [SerializeField] private Vector2 usernameInputSize = new(548f, 76f);
    [SerializeField, Min(1f)] private float usernameTitleFontSize = 42f;
    [SerializeField, Min(1f)] private float usernameInputFontSize = 31f;
    [SerializeField, Min(1f)] private float usernamePlaceholderFontSize = 24f;
    [SerializeField, Range(0.1f, 4f)] private float usernameCaretBlinkRate = 0.85f;

    private readonly List<Action<bool>> waitingForSession = new();
    private bool sessionReady;
    private bool sessionStarting;
    private bool usernameRequestInFlight;
    private string memberId;
    private string lastSessionError;
    private TMP_InputField usernameInput;
    private TMP_Text usernameMessageText;
    private Button usernameConfirmButton;
    private GameObject activeOverlay;
    private RectTransform usernameVisualCaret;
    private TMP_Text usernameInputText;

    public static LeaderboardManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LeaderboardManager existing = FindFirstObjectByType<LeaderboardManager>();
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

    private void Update()
    {
        UpdateUsernameCaret();
    }

    public void PromptForPlayerName(UnityAction onConfirmed)
    {
        PromptForPlayerName(onConfirmed, false);
    }

    /// <summary>Opens the same profile-name screen for starting or editing a run.</summary>
    public void PromptForPlayerName(UnityAction onConfirmed, bool editing)
    {
        CloseOverlay();

        GameObject panel = CreateOverlay(editing ? "Edit Username" : "Username", usernamePromptSize);
        AddText(panel.transform, editing ? "Edit Username" : "Username", usernameTitleFontSize, accentColor, new Vector2(0f, 88f), new Vector2(620f, 54f));

        usernameInput = AddInput(panel.transform, new Vector2(0f, 7f), usernameInputSize);
        usernameInput.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        usernameInput.characterLimit = 16;
        usernameInput.onValidateInput += ValidatePlayerName;
        usernameInput.caretBlinkRate = usernameCaretBlinkRate;
        usernameInput.customCaretColor = true;
        usernameInput.caretColor = caretColor;

        usernameMessageText = AddText(panel.transform, string.Empty, 18f, warningColor, new Vector2(0f, -53f), new Vector2(590f, 30f));
        usernameConfirmButton = AddButton(panel.transform, "Save", new Vector2(0f, -107f), new Vector2(260f, 58f));
        usernameConfirmButton.onClick.AddListener(() => ConfirmPlayerName(onConfirmed));
        usernameInput.onSubmit.AddListener(_ => ConfirmPlayerName(onConfirmed));

        StartCoroutine(FocusUsernameInputNextFrame(usernameInput));
    }

    public void ShowGlobalLeaderboard()
    {
        CloseOverlay();
        SceneManager.LoadScene("LeaderboardScene");
    }

    /// <summary>Reads the configured LootLocker leaderboard for LeaderboardScene.</summary>
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

        // Keep the score until LootLocker explicitly confirms it. A scene
        // transition or brief connection failure cannot silently lose a run.
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

            LootLockerSDKManager.SubmitScore(string.Empty, score, leaderboardKey, GetDisplayName(), response =>
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
        if (usernameRequestInFlight)
        {
            return;
        }

        string candidate = SanitizeName(usernameInput != null ? usernameInput.text : string.Empty);
        if (candidate.Length < 3)
        {
            SetNameMessage("USE AT LEAST 3 CHARACTERS.");
            return;
        }

        if (candidate.IndexOf("player", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SetNameMessage("CHOOSE A NAME WITHOUT 'PLAYER'.");
            return;
        }

        if (!HasLootLockerApiKey())
        {
            SetNameMessage("LOOTLOCKER API KEY IS MISSING.");
            return;
        }

        usernameRequestInFlight = true;
        SetNamePromptBusy(true);
        SetNameMessage("CHECKING NAME...");
        EnsureSession(ready =>
        {
            if (!ready)
            {
                FailNameRequest("COULD NOT CONNECT. TRY AGAIN.");
                return;
            }

            StartCoroutine(ValidateAndSavePlayerName(candidate, onConfirmed));
        });
    }

    private IEnumerator ValidateAndSavePlayerName(string candidate, UnityAction onConfirmed)
    {
        string normalizedCandidate = NormalizeName(candidate);
        string savedNormalizedName = PlayerPrefs.GetString(PlayerNameNormalizedKey, NormalizeName(PlayerPrefs.GetString(PlayerNameKey, string.Empty)));

        bool lookupFinished = false;
        PlayerNameLookupResponse lookupResponse = null;
        LootLockerSDKManager.LookupPlayerNamesByPlayerNames(new[] { candidate }, response =>
        {
            lookupResponse = response;
            lookupFinished = true;
        });

        yield return WaitForRequest(() => lookupFinished);
        if (!lookupFinished || lookupResponse == null || !lookupResponse.success)
        {
            FailNameRequest("NAME CHECK FAILED. TRY AGAIN.");
            yield break;
        }

        if (ContainsClaimedName(lookupResponse.players, normalizedCandidate, savedNormalizedName))
        {
            FailNameRequest("THAT NAME IS ALREADY TAKEN.");
            yield break;
        }

        // LootLocker name lookups protect exact matches. The score-list pass
        // also catches different capitalisation already visible on the board.
        bool scoreListFinished = false;
        LootLockerGetScoreListResponse scoreListResponse = null;
        LootLockerSDKManager.GetScoreList(leaderboardKey, usernameValidationEntryCount, response =>
        {
            scoreListResponse = response;
            scoreListFinished = true;
        });

        yield return WaitForRequest(() => scoreListFinished);
        if (!scoreListFinished || scoreListResponse == null || !scoreListResponse.success)
        {
            FailNameRequest("COULD NOT VERIFY NAME. TRY AGAIN.");
            yield break;
        }

        if (LeaderboardContainsName(scoreListResponse.items, normalizedCandidate, savedNormalizedName))
        {
            FailNameRequest("THAT NAME IS ALREADY TAKEN.");
            yield break;
        }

        bool setNameFinished = false;
        PlayerNameResponse setNameResponse = null;
        LootLockerSDKManager.SetPlayerName(candidate, response =>
        {
            setNameResponse = response;
            setNameFinished = true;
        });

        yield return WaitForRequest(() => setNameFinished);
        if (!setNameFinished || setNameResponse == null || !setNameResponse.success)
        {
            FailNameRequest("COULD NOT SAVE NAME. TRY AGAIN.");
            yield break;
        }

        PlayerPrefs.SetString(PlayerNameKey, candidate);
        PlayerPrefs.SetString(PlayerNameNormalizedKey, normalizedCandidate);
        PlayerPrefs.Save();
        PlayerNameChanged?.Invoke(candidate);
        CloseOverlay();
        onConfirmed?.Invoke();
    }

    private IEnumerator RequestLeaderboardRoutine(Action<bool, LootLockerLeaderboardMember[], string> onComplete)
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

        yield return WaitForRequest(() => sessionFinished);
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

        yield return WaitForRequest(() => scoresFinished);
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

    private IEnumerator WaitForRequest(Func<bool> hasFinished)
    {
        float timeoutAt = Time.realtimeSinceStartup + requestTimeoutSeconds;
        while (!hasFinished() && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }
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
            else
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
        Canvas canvas = FindOverlayCanvas();
        if (canvas == null)
        {
            GameObject canvasObject = new("Runtime UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject overlay = CreateUiObject(name, FixedAspectRatioCanvas.GetContentRoot(canvas));
        overlay.transform.SetAsLastSibling();
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
        panel.AddComponent<Image>().color = panelColor;

        GameObject accent = CreateUiObject("Panel Accent", panel.transform);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.offsetMin = new Vector2(0f, -10f);
        accentRect.offsetMax = Vector2.zero;
        accent.AddComponent<Image>().color = accentColor;

        activeOverlay = overlay;
        return panel;
    }

    private Canvas FindOverlayCanvas()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                return canvas;
            }
        }

        return null;
    }

    private TMP_Text AddText(Transform parent, string value, float fontSize, Color color, Vector2 position, Vector2 size)
    {
        GameObject textObject = CreateUiObject("Text", parent);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = ResolveOverlayFont();
        if (font != null)
        {
            text.font = font;
        }

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

    private TMP_FontAsset ResolveOverlayFont()
    {
        if (boldPixelsFont != null)
        {
            return boldPixelsFont;
        }

        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.font != null)
            {
                return text.font;
            }
        }

        return null;
    }

    private TMP_InputField AddInput(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject inputObject = CreateUiObject("Username Input", parent);
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        inputObject.AddComponent<Image>().color = inputColor;

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        TMP_Text text = AddText(inputObject.transform, string.Empty, usernameInputFontSize, bodyColor, Vector2.zero, size - new Vector2(28f, 0f));
        text.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_Text placeholder = AddText(inputObject.transform, "Enter Your Name", usernamePlaceholderFontSize, placeholderColor, Vector2.zero, size - new Vector2(28f, 0f));
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Alphanumeric;
        input.caretBlinkRate = usernameCaretBlinkRate;
        input.customCaretColor = true;
        input.caretColor = caretColor;

        GameObject caretObject = CreateUiObject("Visible Caret", inputObject.transform);
        Image caretImage = caretObject.AddComponent<Image>();
        caretImage.color = caretColor;
        caretImage.raycastTarget = false;

        RectTransform caretRect = caretObject.GetComponent<RectTransform>();
        caretRect.anchorMin = caretRect.anchorMax = new Vector2(0.5f, 0.5f);
        caretRect.pivot = new Vector2(0.5f, 0.5f);
        caretRect.sizeDelta = new Vector2(usernameCaretWidth, Mathf.Max(12f, text.rectTransform.rect.height * 0.62f));
        caretObject.SetActive(false);

        usernameInputText = text;
        usernameVisualCaret = caretRect;
        return input;
    }

    private void UpdateUsernameCaret()
    {
        if (usernameVisualCaret == null || usernameInput == null || usernameInputText == null)
        {
            return;
        }

        bool isActive = usernameInput.isFocused && usernameInput.interactable;
        if (!isActive)
        {
            if (usernameVisualCaret.gameObject.activeSelf)
            {
                usernameVisualCaret.gameObject.SetActive(false);
            }

            return;
        }

        float blinkRate = Mathf.Max(0.1f, usernameCaretBlinkRate);
        bool isVisible = Mathf.Repeat(Time.unscaledTime * blinkRate, 1f) < 0.5f;
        if (usernameVisualCaret.gameObject.activeSelf != isVisible)
        {
            usernameVisualCaret.gameObject.SetActive(isVisible);
        }

        if (!isVisible)
        {
            return;
        }

        RectTransform textRect = usernameInputText.rectTransform;
        float horizontalPadding = 6f;
        float textWidth = usernameInputText.GetPreferredValues(usernameInput.text).x;
        float maximumWidth = Mathf.Max(0f, textRect.rect.width - horizontalPadding * 2f);
        float caretX = -textRect.rect.width * 0.5f + horizontalPadding + Mathf.Min(textWidth, maximumWidth);
        usernameVisualCaret.anchoredPosition = new Vector2(caretX, 0f);
    }

    private IEnumerator FocusUsernameInputNextFrame(TMP_InputField input)
    {
        // The panel is created during a button event. Waiting one frame stops Unity's
        // event system from replacing the new input field's selection immediately.
        yield return null;

        if (input == null || input != usernameInput || !input.gameObject.activeInHierarchy)
        {
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        input.Select();
        input.ActivateInputField();
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

    private void SetNamePromptBusy(bool busy)
    {
        if (usernameInput != null)
        {
            usernameInput.interactable = !busy;
        }

        if (usernameConfirmButton != null)
        {
            usernameConfirmButton.interactable = !busy;
        }
    }

    private void SetNameMessage(string message)
    {
        if (usernameMessageText != null)
        {
            usernameMessageText.text = message;
        }
    }

    private void FailNameRequest(string message)
    {
        usernameRequestInFlight = false;
        SetNamePromptBusy(false);
        SetNameMessage(message);
    }

    private void CloseOverlay()
    {
        if (activeOverlay != null)
        {
            Destroy(activeOverlay);
        }

        activeOverlay = null;
        usernameInput = null;
        usernameInputText = null;
        usernameVisualCaret = null;
        usernameMessageText = null;
        usernameConfirmButton = null;
        usernameRequestInFlight = false;
    }

    private static bool ContainsClaimedName(PlayerNameWithIDs[] players, string candidate, string savedName)
    {
        if (players == null || candidate == savedName)
        {
            return false;
        }

        foreach (PlayerNameWithIDs player in players)
        {
            if (player != null && NormalizeName(player.name) == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static bool LeaderboardContainsName(LootLockerLeaderboardMember[] entries, string candidate, string savedName)
    {
        if (entries == null || candidate == savedName)
        {
            return false;
        }

        foreach (LootLockerLeaderboardMember entry in entries)
        {
            string name = entry?.player != null && !string.IsNullOrWhiteSpace(entry.player.name)
                ? entry.player.name
                : entry?.metadata;
            if (NormalizeName(name) == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static char ValidatePlayerName(string text, int charIndex, char addedChar)
    {
        return char.IsLetterOrDigit(addedChar) || addedChar == ' ' || addedChar == '-' || addedChar == '_' ? addedChar : '\0';
    }

    private static string SanitizeName(string rawName)
    {
        return string.IsNullOrWhiteSpace(rawName) ? string.Empty : rawName.Trim();
    }

    private static string NormalizeName(string rawName)
    {
        return SanitizeName(rawName).ToLowerInvariant();
    }

    private static string GetError(LootLockerResponse response)
    {
        return response?.errorData?.message ?? "Unknown error";
    }

    private static string GetDisplayName()
    {
        string savedName = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        return string.IsNullOrWhiteSpace(savedName) ? "Username" : savedName;
    }
}
