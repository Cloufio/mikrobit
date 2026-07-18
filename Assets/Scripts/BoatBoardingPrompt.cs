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
    private static readonly Color LabelColor = new Color(0.9f, 0.98f, 1f, 1f);
    private static readonly Color ActionColor = new Color(1f, 0.38f, 0.14f, 1f);
    private static readonly Color KeycapColor = new Color(0.04f, 0.14f, 0.2f, 0.94f);
    private static readonly Color KeycapOutlineColor = new Color(0.55f, 0.94f, 1f, 1f);

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
        TMP_FontAsset font = null;
        Font actionFont = null;
        for (int frame = 0; frame < 30 && (font == null || actionFont == null); frame++)
        {
            // IntroScene uses this TextMesh Pro font from the project's Resources folder.
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            ScoreManager scoreManager = ScoreManager.Instance != null
                ? ScoreManager.Instance
                : FindFirstObjectByType<ScoreManager>();
            actionFont = scoreManager != null && scoreManager.scoreTextElement != null
                ? scoreManager.scoreTextElement.font
                : null;

            if (font == null || actionFont == null)
            {
                yield return null;
            }
        }

        if (font == null || actionFont == null)
        {
            Debug.LogWarning("Boat boarding prompt could not find its UI fonts.", this);
            yield break;
        }

        CreatePrompt(font, actionFont);
        SetVisible(shouldShow);
    }

    private void CreatePrompt(TMP_FontAsset keyFont, Font actionFont)
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
        scaler.dynamicPixelsPerUnit = 16f;
        scaler.referencePixelsPerUnit = 100f;

        promptTransform = promptObject.GetComponent<RectTransform>();
        promptTransform.sizeDelta = new Vector2(390f, 82f);
        baseLocalPosition = new Vector3(0f, 1.45f, 0f);
        promptTransform.localPosition = baseLocalPosition;
        promptTransform.localScale = Vector3.one * 0.011f;

        GameObject keycapObject = new GameObject("E Keycap", typeof(RectTransform), typeof(Image), typeof(Outline));
        keycapObject.transform.SetParent(promptTransform, false);
        RectTransform keycapTransform = keycapObject.GetComponent<RectTransform>();
        keycapTransform.anchoredPosition = new Vector2(-108f, 0f);
        keycapTransform.sizeDelta = new Vector2(70f, 68f);

        keycapImage = keycapObject.GetComponent<Image>();
        keycapImage.color = KeycapColor;
        keycapImage.raycastTarget = false;

        Outline keycapOutline = keycapObject.GetComponent<Outline>();
        keycapOutline.effectColor = KeycapOutlineColor;
        keycapOutline.effectDistance = new Vector2(2f, -2f);

        CreateText(keycapTransform, "Key Label", "E", keyFont, 46, Vector2.zero, new Vector2(70f, 68f), LabelColor, TextAlignmentOptions.Center);
        CreateActionText(promptTransform, actionFont);
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
        keycapColor.a = 0.9f + Mathf.Sin(Time.unscaledTime * 3f) * 0.06f;
        keycapImage.color = keycapColor;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string value,
        TMP_FontAsset font,
        int fontSize,
        Vector2 position,
        Vector2 size,
        Color color,
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
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = color;
        text.raycastTarget = false;

        return text;
    }

    private static void CreateActionText(Transform parent, Font font)
    {
        GameObject textObject = new GameObject("Action Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(70f, 0f);
        rectTransform.sizeDelta = new Vector2(240f, 70f);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = "RIDE BOAT";
        text.fontSize = 42;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = ActionColor;
        text.raycastTarget = false;
    }
}
