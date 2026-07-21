using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the opening tutorial screen and moves the player into MainScene2
/// after they acknowledge it with Enter.
/// </summary>
public class TutorialSceneController : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private Sprite tutorialSprite;
    [SerializeField] private string nextSceneName = "MainScene2";
    [SerializeField] private string beforeKeyText = "PRESS";
    [SerializeField] private string keyText = "ENTER";
    [SerializeField] private string afterKeyText = "TO CONTINUE";
    [SerializeField] private TMP_FontAsset promptFont;

    [Header("Layout")]
    [Range(0.5f, 1f)] [SerializeField] private float artworkScale = 1f;
    [SerializeField] private Vector2 promptMargin = new Vector2(56f, 42f);

    [Header("Style")]
    [SerializeField] private Color backdropColor = new Color(0.01f, 0.05f, 0.08f, 1f);
    [SerializeField] private Color bodyTextColor = new Color(0.94f, 0.99f, 1f, 1f);
    [SerializeField] private Color keycapFill = new Color(0.02f, 0.12f, 0.18f, 0.96f);
    [SerializeField] private Color keycapBorderColor = new Color(0.9f, 0.98f, 1f, 1f);
    [SerializeField, Min(10)] private int bodyFontSize = 28;
    [SerializeField, Min(10)] private int keyFontSize = 21;

    private bool isLoading;
    private Sprite roundedFillSprite;
    private Sprite roundedOutlineSprite;

    private void Start()
    {
        BuildScreen();
    }

    private void Update()
    {
        if (!isLoading && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            isLoading = true;
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void BuildScreen()
    {
        GameObject canvasObject = new GameObject("Tutorial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image backdrop = CreateImage("Backdrop", canvasObject.transform, null, backdropColor);
        Stretch(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CreateArtwork(canvasObject.transform);
        CreateContinuePrompt(canvasObject.transform);
    }

    private void CreateArtwork(Transform parent)
    {
        Image artwork = CreateImage("Tutorial Artwork", parent, tutorialSprite, Color.white);
        artwork.preserveAspect = true;
        artwork.raycastTarget = false;

        RectTransform rect = artwork.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1920f * artworkScale, 1080f * artworkScale);

        if (tutorialSprite == null)
        {
            Debug.LogWarning("TutorialSceneController has no tutorial sprite assigned.", this);
        }
        else
        {
            tutorialSprite.texture.filterMode = FilterMode.Point;
        }
    }

    private void CreateContinuePrompt(Transform parent)
    {
        GameObject prompt = new GameObject("Continue Prompt", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        prompt.transform.SetParent(parent, false);
        RectTransform promptRect = prompt.GetComponent<RectTransform>();
        promptRect.anchorMin = Vector2.one;
        promptRect.anchorMax = Vector2.one;
        promptRect.pivot = Vector2.one;
        promptRect.anchoredPosition = new Vector2(-promptMargin.x, promptMargin.y);
        promptRect.sizeDelta = new Vector2(720f, 72f);

        HorizontalLayoutGroup layout = prompt.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 14f;

        CreatePromptText("Before Key", prompt.transform, beforeKeyText, bodyFontSize);
        CreateKeycap(prompt.transform);
        CreatePromptText("After Key", prompt.transform, afterKeyText, bodyFontSize);
    }

    private void CreateKeycap(Transform parent)
    {
        GameObject keycap = new GameObject("Enter Keycap", typeof(RectTransform), typeof(LayoutElement));
        keycap.transform.SetParent(parent, false);
        LayoutElement layoutElement = keycap.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 116f;
        layoutElement.preferredHeight = 62f;

        Image outerOutline = CreateImage("Outer Keycap Outline", keycap.transform, GetRoundedOutlineSprite(), keycapBorderColor);
        Stretch(outerOutline.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image fill = CreateImage("Keycap Fill", keycap.transform, GetRoundedFillSprite(), keycapFill);
        Stretch(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        Image innerOutline = CreateImage("Inner Keycap Outline", keycap.transform, GetRoundedOutlineSprite(), keycapBorderColor);
        Stretch(innerOutline.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        TextMeshProUGUI keyLabel = CreatePromptText("Key Label", keycap.transform, keyText, keyFontSize);
        keyLabel.alignment = TextAlignmentOptions.Center;
        Stretch(keyLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private TextMeshProUGUI CreatePromptText(string objectName, Transform parent, string value, int fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = promptFont != null ? promptFont : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = bodyTextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        LayoutElement element = textObject.GetComponent<LayoutElement>();
        element.preferredWidth = Mathf.Max(20f, value.Length * fontSize * 0.68f);
        element.preferredHeight = 62f;
        return text;
    }

    private Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = objectName == "Backdrop";
        return image;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private Sprite GetRoundedFillSprite()
    {
        return roundedFillSprite ??= CreateRoundedSprite(false);
    }

    private Sprite GetRoundedOutlineSprite()
    {
        return roundedOutlineSprite ??= CreateRoundedSprite(true);
    }

    private static Sprite CreateRoundedSprite(bool outline)
    {
        const int textureSize = 64;
        const int cornerRadius = 13;
        const int strokeWidth = 4;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool insideOuter = IsInsideRoundedRect(x, y, textureSize, cornerRadius);
                bool insideInner = IsInsideRoundedRect(x, y, textureSize - strokeWidth * 2, cornerRadius - strokeWidth, strokeWidth, strokeWidth);
                texture.SetPixel(x, y, insideOuter && (!outline || !insideInner) ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
    }

    private static bool IsInsideRoundedRect(int x, int y, int size, int radius, int offsetX = 0, int offsetY = 0)
    {
        float localX = x - offsetX;
        float localY = y - offsetY;
        if (localX < 0f || localY < 0f || localX >= size || localY >= size)
        {
            return false;
        }

        float nearestX = Mathf.Clamp(localX, radius, size - radius - 1);
        float nearestY = Mathf.Clamp(localY, radius, size - radius - 1);
        float deltaX = localX - nearestX;
        float deltaY = localY - nearestY;
        return deltaX * deltaX + deltaY * deltaY <= radius * radius;
    }
}
