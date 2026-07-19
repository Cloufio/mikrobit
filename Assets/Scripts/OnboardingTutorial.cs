using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A short, pause-safe introduction to the dock and cleanup loop in MainScene2.
/// </summary>
public class OnboardingTutorial : MonoBehaviour
{
    private const int OverlaySortingOrder = 200;

    private readonly TutorialStep[] steps =
    {
        new TutorialStep(
            "BOARD THE BOAT",
            "Walk down the dock and press E beside the boat to board.",
            "Tutorial/BoardBoat",
            "NEXT"),
        new TutorialStep(
            "CLEAN THE OCEAN",
            "Sail to floating trash and click it to clean the sea and earn score. Avoid dark hazards: they damage the boat.",
            "Tutorial/CleanOcean",
            "START CLEANUP")
    };

    private Canvas tutorialCanvas;
    private Image screenshotImage;
    private TextMeshProUGUI stepText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI continueText;
    private int currentStep;
    private float previousTimeScale;
    private bool isPaused;

    private void Start()
    {
        BuildOverlay();
        ShowStep(0);

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;
    }

    private void Update()
    {
        if (tutorialCanvas == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Advance();
        }
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = previousTimeScale;
        }
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("Onboarding Tutorial", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        tutorialCanvas = canvasObject.GetComponent<Canvas>();
        tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvas.overrideSorting = true;
        tutorialCanvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform dim = CreateImage(canvasObject.transform, "Dim", Vector2.zero, Vector2.zero, new Color(0.01f, 0.05f, 0.09f, 0.76f), true);
        dim.GetComponent<Image>().raycastTarget = true;

        RectTransform panel = CreateImage(canvasObject.transform, "Tutorial Panel", Vector2.zero, new Vector2(1040f, 720f), new Color(0.035f, 0.13f, 0.2f, 0.98f), false);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        RectTransform accent = CreateImage(panel, "Top Accent", new Vector2(0f, 329f), new Vector2(860f, 5f), new Color(0.22f, 0.77f, 0.84f, 1f), false);
        accent.SetAsLastSibling();

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        stepText = CreateText(panel, "Step", font, 22, new Vector2(0f, 295f), new Vector2(780f, 36f), TextAlignmentOptions.Center, new Color(0.37f, 0.82f, 0.88f, 1f));
        titleText = CreateText(panel, "Title", font, 46, new Vector2(0f, 240f), new Vector2(900f, 66f), TextAlignmentOptions.Center, new Color(1f, 0.83f, 0.45f, 1f));

        screenshotImage = CreateImage(panel, "Tutorial Screenshot", new Vector2(0f, 4f), new Vector2(840f, 448f), Color.white, false).GetComponent<Image>();
        screenshotImage.preserveAspect = true;

        descriptionText = CreateText(panel, "Description", font, 27, new Vector2(0f, -254f), new Vector2(810f, 78f), TextAlignmentOptions.Center, new Color(0.86f, 0.95f, 0.98f, 1f));
        descriptionText.textWrappingMode = TextWrappingModes.Normal;

        Button continueButton = CreateButton(panel);
        continueButton.onClick.AddListener(Advance);
        continueText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void ShowStep(int stepIndex)
    {
        currentStep = stepIndex;
        TutorialStep step = steps[stepIndex];

        stepText.text = $"{stepIndex + 1} / {steps.Length}";
        titleText.text = step.title;
        descriptionText.text = step.description;
        continueText.text = step.buttonLabel;

        Texture2D texture = Resources.Load<Texture2D>(step.resourcePath);
        screenshotImage.sprite = texture == null
            ? null
            : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        screenshotImage.enabled = screenshotImage.sprite != null;
    }

    private void Advance()
    {
        if (currentStep + 1 < steps.Length)
        {
            ShowStep(currentStep + 1);
            return;
        }

        isPaused = false;
        Time.timeScale = previousTimeScale;
        Destroy(tutorialCanvas.gameObject);
        tutorialCanvas = null;
    }

    private static RectTransform CreateImage(Transform parent, string objectName, Vector2 position, Vector2 size, Color color, bool stretch)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        if (stretch)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objectName, TMP_FontAsset font, int fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("Continue Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(0.5f, 0.5f);
        buttonTransform.anchorMax = new Vector2(0.5f, 0.5f);
        buttonTransform.pivot = new Vector2(0.5f, 0.5f);
        buttonTransform.anchoredPosition = new Vector2(0f, -322f);
        buttonTransform.sizeDelta = new Vector2(250f, 58f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 0.72f, 0.24f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateText(buttonTransform, "Label", TMP_Settings.defaultFontAsset, 25, Vector2.zero, new Vector2(240f, 50f), TextAlignmentOptions.Center, new Color(0.035f, 0.14f, 0.2f, 1f));
        label.raycastTarget = false;
        return button;
    }

    private readonly struct TutorialStep
    {
        public readonly string title;
        public readonly string description;
        public readonly string resourcePath;
        public readonly string buttonLabel;

        public TutorialStep(string title, string description, string resourcePath, string buttonLabel)
        {
            this.title = title;
            this.description = description;
            this.resourcePath = resourcePath;
            this.buttonLabel = buttonLabel;
        }
    }
}
