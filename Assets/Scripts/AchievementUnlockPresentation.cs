using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shows a short, pause-the-game celebration whenever a new microplastic
/// combo achievement is unlocked. It is created at runtime so no scene setup
/// is required and works from every gameplay scene.
/// </summary>
public sealed class AchievementUnlockPresentation : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField] private float cardFloatDistance = 14f;
    [SerializeField] private float cardFloatSpeed = 2f;

    private static AchievementUnlockPresentation instance;

    private readonly Queue<MicroplasticComboTracker.ComboDefinition> pendingUnlocks = new();

    private GameObject overlay;
    private GameObject continueButton;
    private TextMeshProUGUI achievementNameText;
    private TextMeshProUGUI funFactText;
    private Image achievementCardImage;
    private GameObject fallbackCardContent;
    private Outline fallbackCardOutline;
    private RectTransform cardRect;
    private Vector2 cardBasePosition;

    private MicroplasticComboTracker.ComboDefinition activeUnlock;
    private float typewriterProgress;
    private bool isTyping;
    private bool hasPausedGame;
    private float previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePresentation()
    {
        if (instance != null)
        {
            return;
        }

        var presentationObject = new GameObject("Achievement Unlock Presentation");
        presentationObject.AddComponent<AchievementUnlockPresentation>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        MicroplasticComboTracker.ComboUnlocked += QueueUnlock;
    }

    private void OnDisable()
    {
        MicroplasticComboTracker.ComboUnlocked -= QueueUnlock;
    }

    private void Update()
    {
        if (overlay == null || !overlay.activeSelf)
        {
            return;
        }

        if (cardRect != null)
        {
            var floatOffset = Mathf.Sin(Time.unscaledTime * cardFloatSpeed) * cardFloatDistance;
            cardRect.anchoredPosition = cardBasePosition + Vector2.up * floatOffset;
        }

        if (isTyping)
        {
            TypeFunFact();
        }

        if (!isTyping && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)))
        {
            ContinueGame();
        }
    }

    private void QueueUnlock(MicroplasticComboTracker.ComboDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        pendingUnlocks.Enqueue(definition);
        if (activeUnlock == null)
        {
            ShowNextUnlock();
        }
    }

    private void ShowNextUnlock()
    {
        if (pendingUnlocks.Count == 0)
        {
            activeUnlock = null;
            HideAndResumeGame();
            return;
        }

        activeUnlock = pendingUnlocks.Dequeue();
        PauseGame();
        EnsureOverlay();

        overlay.SetActive(true);
        achievementNameText.text = activeUnlock.title.ToUpperInvariant();
        Sprite artwork = MainSceneAchievementArtworkLibrary.GetArtwork(activeUnlock.id);
        achievementCardImage.sprite = artwork;
        achievementCardImage.color = Color.white;
        achievementCardImage.preserveAspect = artwork != null;
        fallbackCardContent.SetActive(artwork == null);
        fallbackCardOutline.enabled = artwork == null;
        funFactText.text = string.Empty;
        typewriterProgress = 0f;
        isTyping = true;
        continueButton.SetActive(false);
    }

    private void PauseGame()
    {
        if (hasPausedGame)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        hasPausedGame = true;
    }

    private void HideAndResumeGame()
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
        }

        if (!hasPausedGame)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        hasPausedGame = false;
    }

    private void TypeFunFact()
    {
        string completeFact = activeUnlock.funFact ?? string.Empty;
        typewriterProgress += Time.unscaledDeltaTime * MainSceneAchievementArtworkLibrary.GetFunFactCharactersPerSecond();
        int visibleCharacterCount = Mathf.Min(completeFact.Length, Mathf.FloorToInt(typewriterProgress));
        funFactText.text = completeFact.Substring(0, visibleCharacterCount);

        if (visibleCharacterCount < completeFact.Length)
        {
            return;
        }

        isTyping = false;
        continueButton.SetActive(true);
    }

    private void ContinueGame()
    {
        if (isTyping)
        {
            return;
        }

        overlay.SetActive(false);
        activeUnlock = null;

        if (pendingUnlocks.Count > 0)
        {
            ShowNextUnlock();
            return;
        }

        HideAndResumeGame();
    }

    private void EnsureOverlay()
    {
        if (overlay != null)
        {
            return;
        }

        EnsureEventSystem();

        overlay = new GameObject("Achievement Unlock Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(overlay);

        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        StretchToParent(overlay.GetComponent<RectTransform>());

        var backdrop = CreateImage("Blackout", overlay.transform, new Color(0.01f, 0.02f, 0.04f, 0.96f));
        StretchToParent(backdrop.rectTransform);

        var content = new GameObject("Unlock Content", typeof(RectTransform));
        content.transform.SetParent(overlay.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(1500f, 820f);
        // Raise the whole presentation so its header and button have balanced top/bottom margins.
        contentRect.anchoredPosition = new Vector2(-70f, 55f);

        var header = CreateText("Header", content.transform, "Anda Mendapatkan Achivement Baru", 52, new Color(1f, 0.71f, 0.27f));
        header.alignment = TextAlignmentOptions.Center;
        header.fontStyle = FontStyles.Bold;
        SetRect(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1400f, 80f), new Vector2(0f, -40f));
        AddShadow(header, new Color(0.04f, 0.08f, 0.16f, 1f), new Vector2(5f, -5f));

        var card = CreateImage("Unlocked Card", content.transform, new Color(0.04f, 0.07f, 0.13f, 1f));
        achievementCardImage = card;
        achievementCardImage.preserveAspect = true;
        cardRect = card.rectTransform;
        SetRect(cardRect, new Vector2(0.22f, 0.47f), new Vector2(0.22f, 0.47f), new Vector2(0.5f, 0.5f), new Vector2(430f, 590f), Vector2.zero);
        cardBasePosition = cardRect.anchoredPosition;

        fallbackCardContent = new GameObject("Fallback Card Content", typeof(RectTransform));
        fallbackCardContent.transform.SetParent(card.transform, false);
        StretchToParent(fallbackCardContent.GetComponent<RectTransform>());
        fallbackCardOutline = card.gameObject.AddComponent<Outline>();
        fallbackCardOutline.effectColor = new Color(1f, 0.66f, 0.22f, 1f);
        fallbackCardOutline.effectDistance = new Vector2(8f, 8f);

        var cardGlow = CreateImage("Card Glow", fallbackCardContent.transform, new Color(1f, 0.65f, 0.18f, 0.18f));
        SetRect(cardGlow.rectTransform, new Vector2(0.5f, 0.81f), new Vector2(0.5f, 0.81f), new Vector2(0.5f, 0.5f), new Vector2(300f, 12f), Vector2.zero);

        achievementNameText = CreateText("Card Title", fallbackCardContent.transform, string.Empty, 44, new Color(1f, 0.76f, 0.38f));
        achievementNameText.alignment = TextAlignmentOptions.Center;
        achievementNameText.enableWordWrapping = true;
        achievementNameText.fontStyle = FontStyles.Bold;
        SetRect(achievementNameText.rectTransform, new Vector2(0.5f, 0.61f), new Vector2(0.5f, 0.61f), new Vector2(0.5f, 0.5f), new Vector2(360f, 150f), Vector2.zero);
        AddShadow(achievementNameText, new Color(0f, 0f, 0f, 0.9f), new Vector2(4f, -4f));

        var cardLabel = CreateText("Card Label", fallbackCardContent.transform, "NEW ACHIEVEMENT", 30, Color.white);
        cardLabel.alignment = TextAlignmentOptions.Center;
        SetRect(cardLabel.rectTransform, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(360f, 50f), Vector2.zero);

        var rightPanel = CreateImage("Fun Fact Panel", content.transform, new Color(0.05f, 0.11f, 0.20f, 0.94f));
        SetRect(rightPanel.rectTransform, new Vector2(0.69f, 0.46f), new Vector2(0.69f, 0.46f), new Vector2(0.5f, 0.5f), new Vector2(840f, 590f), Vector2.zero);
        AddOutline(rightPanel, new Color(0.18f, 0.42f, 0.59f, 1f), new Vector2(3f, 3f));

        TMP_FontAsset boldPixelsFont = MainSceneAchievementArtworkLibrary.GetBoldPixelsFont();

        var factHeading = CreateText("Fun Fact Heading", rightPanel.transform, "Fun Fact", 80, Color.white);
        ApplyWhitePixelStyle(factHeading, boldPixelsFont);
        factHeading.fontStyle = FontStyles.Normal;
        factHeading.alignment = TextAlignmentOptions.Center;
        SetRect(factHeading.rectTransform, new Vector2(0f, 0.70f), new Vector2(1f, 0.96f), new Vector2(0.5f, 0.5f), new Vector2(-90f, 0f), Vector2.zero);
        AddShadow(factHeading, Color.black, new Vector2(4f, -4f));

        funFactText = CreateText("Fun Fact Text", rightPanel.transform, string.Empty, 48, Color.white);
        ApplyWhitePixelStyle(funFactText, boldPixelsFont);
        funFactText.fontStyle = FontStyles.Normal;
        funFactText.alignment = TextAlignmentOptions.Top;
        funFactText.enableWordWrapping = true;
        // Indonesian facts vary greatly in length. Start at 48, then shrink only
        // as much as needed to keep every line inside the panel.
        funFactText.enableAutoSizing = true;
        funFactText.fontSizeMin = 20f;
        funFactText.fontSizeMax = 48f;
        funFactText.overflowMode = TextOverflowModes.Ellipsis;
        SetRect(funFactText.rectTransform, new Vector2(0f, 0.18f), new Vector2(1f, 0.65f), new Vector2(0.5f, 0.5f), new Vector2(-90f, 0f), Vector2.zero);
        AddShadow(funFactText, Color.black, new Vector2(4f, -4f));

        continueButton = CreateButton(content.transform, "Lanjut", ContinueGame);
        var buttonRect = continueButton.GetComponent<RectTransform>();
        SetRect(buttonRect, new Vector2(0.69f, 0f), new Vector2(0.69f, 0f), new Vector2(0.5f, 0.5f), new Vector2(330f, 88f), new Vector2(0f, 12f));

        overlay.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystem = new GameObject("Achievement Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, Color color)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var textComponent = textObject.GetComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.raycastTarget = false;
        textComponent.enableAutoSizing = false;
        return textComponent;
    }

    private static void ApplyWhitePixelStyle(TextMeshProUGUI text, TMP_FontAsset font)
    {
        if (font == null)
        {
            return;
        }

        text.font = font;
        text.color = Color.white;

        Material material = new(font.material);
        material.SetColor("_FaceColor", Color.white);
        material.SetFloat("_OutlineWidth", 0f);
        material.DisableKeyword("UNDERLAY_ON");
        text.fontMaterial = material;
    }

    private static GameObject CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject("Continue Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.96f, 0.50f, 0.16f, 1f);
        AddOutline(image, new Color(1f, 0.82f, 0.42f, 1f), new Vector2(3f, 3f));

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 0.58f, 1f);
        colors.pressedColor = new Color(0.78f, 0.32f, 0.10f, 1f);
        button.colors = colors;

        var labelText = CreateText("Text", buttonObject.transform, label, 34, new Color(0.07f, 0.10f, 0.16f));
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        StretchToParent(labelText.rectTransform);
        return buttonObject;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
    {
        var outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static void AddShadow(Graphic graphic, Color color, Vector2 distance)
    {
        var shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }
}
