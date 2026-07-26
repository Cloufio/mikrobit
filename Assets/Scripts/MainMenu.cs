using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Renamed Button Labels")]
    [SerializeField] private Color labelCoverColor = new Color(0.83f, 0.42f, 0.28f, 1f);
    [SerializeField] private Color labelColor = new Color(1f, 0.91f, 0.67f, 1f);
    [SerializeField] private float renamedButtonFontSize = 27f;

    [Header("Player Name Widget")]
    [Tooltip("Optional. Leave empty to reuse the existing BoldPixels font in MainMenu.")]
    [SerializeField] private TMP_FontAsset playerNameFont;
    [SerializeField] private Color playerNamePanelColor = new Color(0.04f, 0.12f, 0.20f, 0.94f);
    [SerializeField] private Color playerNameLabelColor = new Color(1f, 0.68f, 0.24f, 1f);
    [SerializeField] private Color playerNameValueColor = new Color(0.95f, 0.98f, 1f, 1f);
    [SerializeField] private Vector2 playerNameWidgetPosition = new Vector2(-42f, -34f);
    [SerializeField] private Vector2 playerNameWidgetSize = new Vector2(420f, 100f);
    [SerializeField] private float playerNameLabelFontSize = 24f;
    [SerializeField] private float playerNameValueFontSize = 34f;

    private TMP_Text playerNameText;
    private LeaderboardManager leaderboardManager;

    private void Awake()
    {
        ConfigureRenamedButton("AchievementsButton", "Achievements");
        ConfigureRenamedButton("LeaderboardsButton", "Leaderboards");
        ConfigurePlayerNameWidget();
    }

    private void OnEnable()
    {
        leaderboardManager = LeaderboardManager.EnsureInstance();
        leaderboardManager.PlayerNameChanged += RefreshPlayerNameWidget;
        RefreshPlayerNameWidget(leaderboardManager.CurrentPlayerName);
    }

    private void OnDisable()
    {
        if (leaderboardManager != null)
        {
            leaderboardManager.PlayerNameChanged -= RefreshPlayerNameWidget;
            leaderboardManager = null;
        }
    }

    public void playGame()
    {
        LeaderboardManager.EnsureInstance().PromptForPlayerName(() => SceneManager.LoadScene("IntroScene"));
    }

    public void EditPlayerName()
    {
        LeaderboardManager.EnsureInstance().PromptForPlayerName(() => RefreshPlayerNameWidget(LeaderboardManager.EnsureInstance().CurrentPlayerName), true);
    }

    public void ShowAchievements()
    {
        SceneManager.LoadScene("AchievementScene");
    }

    public void ShowLeaderboards()
    {
        SceneManager.LoadScene("LeaderboardScene");
    }

    private void ConfigureRenamedButton(string buttonName, string label)
    {
        Transform buttonTransform = transform.Find(buttonName);
        if (buttonTransform == null)
        {
            Debug.LogWarning($"MainMenu could not find '{buttonName}'.", this);
            return;
        }

        RectTransform buttonRect = buttonTransform.GetComponent<RectTransform>();
        TMP_Text labelText = buttonTransform.GetComponentInChildren<TMP_Text>(true);
        if (buttonRect == null || labelText == null)
        {
            Debug.LogWarning($"MainMenu button '{buttonName}' is missing its UI components.", this);
            return;
        }

        Image coverImage = GetOrCreateLabelCover(buttonRect);
        RectTransform coverRect = coverImage.rectTransform;
        coverRect.anchorMin = new Vector2(0.5f, 0.5f);
        coverRect.anchorMax = new Vector2(0.5f, 0.5f);
        coverRect.pivot = new Vector2(0.5f, 0.5f);
        coverRect.anchoredPosition = Vector2.zero;
        coverRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x - 32f, buttonRect.sizeDelta.y - 34f);
        coverImage.color = labelCoverColor;

        labelText.gameObject.SetActive(true);
        labelText.transform.SetAsLastSibling();
        labelText.text = label;
        labelText.fontSize = renamedButtonFontSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = labelColor;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Overflow;

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 0f);
        labelRect.offsetMax = new Vector2(-20f, 0f);
    }

    private static Image GetOrCreateLabelCover(RectTransform buttonRect)
    {
        Transform coverTransform = buttonRect.Find("Label Cover");
        if (coverTransform != null)
        {
            return coverTransform.GetComponent<Image>();
        }

        GameObject coverObject = new GameObject("Label Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        coverObject.transform.SetParent(buttonRect, false);
        coverObject.transform.SetAsFirstSibling();

        Image coverImage = coverObject.GetComponent<Image>();
        coverImage.raycastTarget = false;
        return coverImage;
    }

    private void ConfigurePlayerNameWidget()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform contentRoot = FixedAspectRatioCanvas.GetContentRoot(canvas);
        Transform existing = contentRoot.Find("Player Name Widget");
        GameObject widgetObject;
        if (existing != null)
        {
            widgetObject = existing.gameObject;
        }
        else
        {
            widgetObject = new GameObject("Player Name Widget", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            widgetObject.transform.SetParent(contentRoot, false);
        }

        RectTransform widget = widgetObject.GetComponent<RectTransform>();
        widget.anchorMin = new Vector2(1f, 1f);
        widget.anchorMax = new Vector2(1f, 1f);
        widget.pivot = new Vector2(1f, 1f);
        widget.anchoredPosition = playerNameWidgetPosition;
        widget.sizeDelta = playerNameWidgetSize;

        Image panel = widgetObject.GetComponent<Image>();
        panel.color = playerNamePanelColor;
        Button button = widgetObject.GetComponent<Button>();
        button.targetGraphic = panel;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(EditPlayerName);

        TMP_Text label = CreateWidgetText(widget, "Label", "USERNAME", playerNameLabelFontSize, playerNameLabelColor);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(20f, 19f);
        labelRect.sizeDelta = new Vector2(playerNameWidgetSize.x - 40f, 34f);
        label.alignment = TextAlignmentOptions.MidlineLeft;

        playerNameText = CreateWidgetText(widget, "Value", "SET USERNAME", playerNameValueFontSize, playerNameValueColor);
        RectTransform valueRect = playerNameText.rectTransform;
        valueRect.anchorMin = new Vector2(0f, 0.5f);
        valueRect.anchorMax = new Vector2(0f, 0.5f);
        valueRect.pivot = new Vector2(0f, 0.5f);
        valueRect.anchoredPosition = new Vector2(20f, -20f);
        valueRect.sizeDelta = new Vector2(playerNameWidgetSize.x - 40f, 42f);
        playerNameText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private TMP_Text CreateWidgetText(Transform parent, string objectName, string value, float fontSize, Color color)
    {
        Transform existing = parent.Find(objectName);
        GameObject textObject;
        if (existing != null)
        {
            textObject = existing.gameObject;
        }
        else
        {
            textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
        }

        TMP_Text text = textObject.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = playerNameFont != null ? playerNameFont : FindMenuFont();
        if (font != null)
        {
            text.font = font;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private TMP_FontAsset FindMenuFont()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text != null && text.font != null && text.gameObject.name != "Label" && text.gameObject.name != "Value")
            {
                return text.font;
            }
        }

        return null;
    }

    private void RefreshPlayerNameWidget(string playerName)
    {
        if (playerNameText == null)
        {
            ConfigurePlayerNameWidget();
        }

        if (playerNameText != null)
        {
            playerNameText.text = string.IsNullOrWhiteSpace(playerName) || playerName == "USERNAME" ? "SET USERNAME" : playerName;
        }
    }
}
