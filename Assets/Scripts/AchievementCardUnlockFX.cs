using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds a gentle presentation effect to an unlocked achievement card.
/// Attach this to the card parent and assign its visual image layers.
/// </summary>
public class AchievementCardUnlockFX : MonoBehaviour
{
    [Header("Card Layers")]
    [SerializeField] private RectTransform[] cardLayers;

    [Header("Unlock Motion")]
    [SerializeField] private bool unlocked = true;
    [SerializeField] private float floatHeight = 12f;
    [SerializeField] private float floatSpeed = 1.25f;
    [SerializeField] private float wobbleAngle = 1.6f;
    [SerializeField] private float wobbleSpeed = 1.1f;

    [Header("Gold Sparkles")]
    [SerializeField, Range(4, 32)] private int sparkleCount = 14;
    [SerializeField] private Color sparkleColor = new Color(1f, 0.75f, 0.2f, 1f);
    [SerializeField] private Vector2 sparkleSizeRange = new Vector2(4f, 9f);
    [SerializeField] private float sparkleSpeed = 1.8f;
    [SerializeField] private float sparklePadding = 26f;

    [Header("Card Flip Details")]
    [SerializeField] private bool canOpenDetails = true;
    [SerializeField] private float flipDuration = 0.32f;
    [SerializeField] private string cardTitle = "THE FOOL";
    [SerializeField, TextArea(2, 3)] private string unlockText = "UNLOCKED BY\nCLEANING YOUR FIRST PIECE OF TRASH.";
    [SerializeField, TextArea(2, 3)] private string symbolismText = "SYMBOLIZES\nA NEW BEGINNING FOR THE SEA.";
    [SerializeField] private TMP_FontAsset detailFont;
    [SerializeField] private Color detailBackground = new Color(0.06f, 0.04f, 0.08f, 1f);
    [SerializeField] private Color detailBorder = new Color(1f, 0.75f, 0.2f, 1f);
    [SerializeField] private Color detailText = new Color(1f, 0.91f, 0.68f, 1f);

    private readonly List<Vector2> basePositions = new();
    private readonly List<float> sparklePhases = new();
    private readonly List<Image> sparkles = new();
    private readonly List<Graphic> faceGraphics = new();
    private Vector2 cardCenter;
    private Vector2 cardSize;
    private RectTransform detailPanel;
    private float flipAngle;
    private float targetFlipAngle;
    private bool detailsShown;

    private void Awake()
    {
        foreach (RectTransform layer in cardLayers)
        {
            if (layer != null)
            {
                basePositions.Add(layer.anchoredPosition);
                faceGraphics.AddRange(layer.GetComponentsInChildren<Graphic>(true));
            }
        }
    }

    private void Start()
    {
        if (!unlocked || cardLayers == null || cardLayers.Length == 0 || cardLayers[0] == null)
        {
            enabled = false;
            return;
        }

        cardCenter = cardLayers[0].anchoredPosition;
        cardSize = cardLayers[0].rect.size;
        CreateSparkles();
        CreateDetailPanel();
    }

    private void OnEnable()
    {
        SetSparkleVisibility(true);
    }

    private void OnDisable()
    {
        SetSparkleVisibility(false);

        detailsShown = false;
        flipAngle = 0f;
        targetFlipAngle = 0f;

        if (detailPanel != null)
        {
            detailPanel.gameObject.SetActive(false);
        }

        foreach (Graphic graphic in faceGraphics)
        {
            if (graphic != null)
            {
                graphic.enabled = true;
            }
        }
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float verticalOffset = Mathf.Sin(time * floatSpeed) * floatHeight;
        float rotation = Mathf.Sin(time * wobbleSpeed) * wobbleAngle;
        UpdateCardFlip();
        int positionIndex = 0;

        foreach (RectTransform layer in cardLayers)
        {
            if (layer == null)
            {
                continue;
            }

            layer.anchoredPosition = basePositions[positionIndex++] + Vector2.up * verticalOffset;
            layer.localRotation = Quaternion.Euler(0f, 0f, rotation);
            layer.localScale = new Vector3(GetFaceScale(), 1f, 1f);
        }

        if (detailPanel != null)
        {
            detailPanel.anchoredPosition = cardCenter + Vector2.up * verticalOffset;
            detailPanel.localRotation = Quaternion.Euler(0f, 0f, rotation);
            detailPanel.localScale = new Vector3(GetDetailScale(), 1f, 1f);
        }

        for (int i = 0; i < sparkles.Count; i++)
        {
            float pulse = Mathf.Clamp01(Mathf.Sin(time * sparkleSpeed + sparklePhases[i]) * 0.7f + 0.35f);
            Color color = sparkleColor;
            color.a = pulse;
            sparkles[i].color = color;
            sparkles[i].rectTransform.localScale = Vector3.one * (0.7f + pulse * 0.55f);
        }

        if (canOpenDetails && unlocked && !Mathf.Approximately(GetFaceScale(), 0f) && Input.GetMouseButtonDown(0)
            && RectTransformUtility.RectangleContainsScreenPoint(cardLayers[0], Input.mousePosition, null))
        {
            ToggleDetails();
        }
    }

    /// <summary>Called from the Inspector or by clicking the unlocked card.</summary>
    public void ToggleDetails()
    {
        if (!canOpenDetails || !unlocked || detailPanel == null)
        {
            return;
        }

        detailsShown = !detailsShown;
        targetFlipAngle = detailsShown ? 180f : 0f;
    }

    private void CreateSparkles()
    {
        System.Random random = new System.Random(gameObject.GetInstanceID());

        for (int i = 0; i < sparkleCount; i++)
        {
            GameObject sparkleObject = new GameObject($"Gold Sparkle {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform sparkleTransform = sparkleObject.GetComponent<RectTransform>();
            sparkleTransform.SetParent(transform, false);
            sparkleTransform.anchorMin = new Vector2(0.5f, 0.5f);
            sparkleTransform.anchorMax = new Vector2(0.5f, 0.5f);
            sparkleTransform.pivot = new Vector2(0.5f, 0.5f);

            float angle = (Mathf.PI * 2f / sparkleCount) * i + RandomRange(random, -0.2f, 0.2f);
            float radiusX = cardSize.x * 0.5f + sparklePadding + RandomRange(random, 0f, 22f);
            float radiusY = cardSize.y * 0.5f + sparklePadding + RandomRange(random, 0f, 22f);
            sparkleTransform.anchoredPosition = cardCenter + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            sparkleTransform.sizeDelta = Vector2.one * RandomRange(random, sparkleSizeRange.x, sparkleSizeRange.y);

            Image sparkle = sparkleObject.GetComponent<Image>();
            sparkle.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            sparkle.raycastTarget = false;
            sparkle.color = sparkleColor;
            sparkles.Add(sparkle);
            sparklePhases.Add(RandomRange(random, 0f, Mathf.PI * 2f));
        }
    }

    private void CreateDetailPanel()
    {
        if (!canOpenDetails || cardLayers[0] == null)
        {
            return;
        }

        Transform parent = cardLayers[0].parent;
        GameObject panelObject = new GameObject("Achievement Details", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        detailPanel = panelObject.GetComponent<RectTransform>();
        detailPanel.SetParent(parent, false);
        detailPanel.anchorMin = new Vector2(0.5f, 0.5f);
        detailPanel.anchorMax = new Vector2(0.5f, 0.5f);
        detailPanel.pivot = new Vector2(0.5f, 0.5f);
        detailPanel.sizeDelta = cardSize + new Vector2(12f, 12f);
        detailPanel.anchoredPosition = cardCenter;
        detailPanel.SetAsLastSibling();

        Image border = panelObject.GetComponent<Image>();
        border.color = detailBorder;
        border.raycastTarget = false;

        RectTransform inner = CreatePanelLayer("Detail Background", detailPanel, detailBackground, new Vector2(12f, 12f));
        TMP_FontAsset font = ResolveDetailFont();
        CreateDetailLabel("Title", inner, cardTitle, new Vector2(0.5f, 0.74f), 34f, FontStyles.Bold, detailText);
        CreateDetailLabel("Unlock Requirement", inner, unlockText, new Vector2(0.5f, 0.51f), 19f, FontStyles.Normal, detailText);
        CreateDetailLabel("Meaning", inner, symbolismText, new Vector2(0.5f, 0.28f), 19f, FontStyles.Normal, detailText);

        foreach (TextMeshProUGUI label in inner.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.font = font;
        }

        detailPanel.gameObject.SetActive(false);
    }

    private static RectTransform CreatePanelLayer(string name, RectTransform parent, Color color, Vector2 inset)
    {
        GameObject layerObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform layer = layerObject.GetComponent<RectTransform>();
        layer.SetParent(parent, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = inset * 0.5f;
        layer.offsetMax = -inset * 0.5f;

        Image image = layerObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return layer;
    }

    private static void CreateDetailLabel(string name, RectTransform parent, string text, Vector2 anchor, float size, FontStyles style, Color color)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.SetParent(parent, false);
        labelTransform.anchorMin = anchor;
        labelTransform.anchorMax = anchor;
        labelTransform.pivot = new Vector2(0.5f, 0.5f);
        labelTransform.sizeDelta = new Vector2(280f, 120f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
    }

    private TMP_FontAsset ResolveDetailFont()
    {
        if (detailFont != null)
        {
            return detailFont;
        }

        TMP_Text sceneLabel = GetComponentInParent<Canvas>().GetComponentInChildren<TMP_Text>(true);
        return sceneLabel != null ? sceneLabel.font : TMP_Settings.defaultFontAsset;
    }

    private void UpdateCardFlip()
    {
        flipAngle = Mathf.MoveTowards(flipAngle, targetFlipAngle, 180f / Mathf.Max(flipDuration, 0.01f) * Time.unscaledDeltaTime);
        bool showFront = Mathf.Cos(flipAngle * Mathf.Deg2Rad) >= 0f;

        foreach (Graphic graphic in faceGraphics)
        {
            if (graphic != null)
            {
                graphic.enabled = showFront;
            }
        }

        if (detailPanel != null)
        {
            detailPanel.gameObject.SetActive(!showFront);
        }
    }

    private float GetFaceScale()
    {
        return Mathf.Abs(Mathf.Cos(flipAngle * Mathf.Deg2Rad));
    }

    private float GetDetailScale()
    {
        return Mathf.Abs(Mathf.Cos(flipAngle * Mathf.Deg2Rad));
    }

    private void SetSparkleVisibility(bool visible)
    {
        foreach (Image sparkle in sparkles)
        {
            if (sparkle != null)
            {
                sparkle.gameObject.SetActive(visible);
            }
        }
    }

    private static float RandomRange(System.Random random, float minimum, float maximum)
    {
        return minimum + (float)random.NextDouble() * (maximum - minimum);
    }
}
