using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a small, world-space boarding hint above the boat while its trigger is active.
/// </summary>
public class BoatBoardingPrompt : MonoBehaviour
{
    private const int PromptSortingOrder = 20;
    private static readonly Color PlateColor = new Color(0.025f, 0.14f, 0.22f, 0.94f);
    private static readonly Color KeycapColor = new Color(1f, 0.76f, 0.25f, 1f);
    private static readonly Color DarkBlue = new Color(0.035f, 0.18f, 0.27f, 1f);
    private static readonly Color BodyTextColor = new Color(0.84f, 0.95f, 0.98f, 1f);

    private Canvas promptCanvas;
    private RectTransform promptTransform;
    private Image keycapImage;
    private Vector3 baseLocalPosition;
    private bool shouldShow;

    private void Awake()
    {
        StartCoroutine(BuildPrompt());
    }

    public void SetVisible(bool visible)
    {
        shouldShow = visible;
        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(visible);
        }
    }

    private IEnumerator BuildPrompt()
    {
        TMP_FontAsset normalFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        normalFont ??= TMP_Settings.defaultFontAsset;

        Font boldPixelFont = null;
        for (int frame = 0; frame < 30 && boldPixelFont == null; frame++)
        {
            ScoreManager scoreManager = ScoreManager.Instance != null
                ? ScoreManager.Instance
                : FindFirstObjectByType<ScoreManager>();
            boldPixelFont = scoreManager != null && scoreManager.scoreTextElement != null
                ? scoreManager.scoreTextElement.font
                : null;

            if (boldPixelFont == null)
            {
                yield return null;
            }
        }

        if (normalFont == null || boldPixelFont == null)
        {
            Debug.LogWarning("Boat boarding prompt could not find its fonts.", this);
            yield break;
        }

        CreatePrompt(normalFont, boldPixelFont);
        SetVisible(shouldShow);
    }

    private void CreatePrompt(TMP_FontAsset normalFont, Font boldPixelFont)
    {
        GameObject promptObject = new GameObject("Board Boat Prompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        promptObject.transform.SetParent(transform, false);

        promptCanvas = promptObject.GetComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.worldCamera = Camera.main;
        promptCanvas.overrideSorting = true;
        promptCanvas.sortingOrder = PromptSortingOrder;
        promptCanvas.pixelPerfect = true;

        CanvasScaler scaler = promptObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 32f;
        scaler.referencePixelsPerUnit = 32f;

        promptTransform = promptObject.GetComponent<RectTransform>();
        promptTransform.sizeDelta = new Vector2(330f, 58f);
        baseLocalPosition = new Vector3(0f, 1.35f, 0f);
        promptTransform.localPosition = baseLocalPosition;
        promptTransform.localScale = Vector3.one * 0.0125f;

        CreateImage(promptTransform, "Prompt Plate", Vector2.zero, new Vector2(330f, 58f), PlateColor);

        GameObject keycapObject = new GameObject("E Keycap", typeof(RectTransform), typeof(Image));
        keycapObject.transform.SetParent(promptTransform, false);
        RectTransform keycapTransform = keycapObject.GetComponent<RectTransform>();
        keycapTransform.anchoredPosition = new Vector2(-47f, 0f);
        keycapTransform.sizeDelta = new Vector2(48f, 44f);

        keycapImage = keycapObject.GetComponent<Image>();
        keycapImage.color = KeycapColor;
        keycapImage.raycastTarget = false;

        CreateText(keycapTransform, "Key Label", "E", boldPixelFont, 34, Vector2.zero, new Vector2(48f, 44f), DarkBlue, TextAnchor.MiddleCenter);
        CreateNormalText(promptTransform, "Press Label", "Press", normalFont, new Vector2(-120f, 0f), new Vector2(72f, 44f), TextAlignmentOptions.MidlineRight);
        CreateNormalText(promptTransform, "Action Label", "to Ride Boat", normalFont, new Vector2(64f, 0f), new Vector2(150f, 44f), TextAlignmentOptions.MidlineLeft);
    }

    private void Update()
    {
        if (promptCanvas == null || !promptCanvas.gameObject.activeSelf)
        {
            return;
        }

        float pulse = Mathf.Sin(Time.unscaledTime * 3f) * 0.035f;
        promptTransform.localPosition = baseLocalPosition + Vector3.up * pulse;

        Color keycapColor = KeycapColor;
        keycapColor.a = 0.92f + Mathf.Sin(Time.unscaledTime * 3f) * 0.08f;
        keycapImage.color = keycapColor;
    }

    private static Image CreateImage(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(
        Transform parent,
        string objectName,
        string value,
        Font font,
        int fontSize,
        Vector2 position,
        Vector2 size,
        Color color,
        TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = color;
        text.raycastTarget = false;

        return text;
    }

    private static TextMeshProUGUI CreateNormalText(
        Transform parent,
        string objectName,
        string value,
        TMP_FontAsset font,
        Vector2 position,
        Vector2 size,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = 24;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = BodyTextColor;
        text.raycastTarget = false;
        return text;
    }
}
