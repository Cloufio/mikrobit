using System;
using TMPro;
using UnityEngine;

/// <summary>
/// A world-rendered boat interaction prompt configured from BoatController's Inspector.
/// </summary>
public class BoatBoardingPrompt : MonoBehaviour
{
    // TextMeshPro's world glyphs are much smaller than the virtual pixel units
    // used by the prompt's keycap and offsets.
    private const float WorldTextUnitScale = 20f;

    [Serializable]
    public class Style
    {
        [Header("Copy")]
        public string beforeKeyText = "Press";
        public string keyText = "E";
        public string afterKeyText = "to interact";

        [Header("Position")]
        public Vector3 localOffset = new Vector3(0f, 1.35f, 0f);
        [Min(0.001f)] public float worldScale = 1f;
        [Min(0)] public int sortingOrder = 20;

        [Header("Typography")]
        [Min(1)] public int bodyFontSize = 34;
        [Min(1)] public int keyFontSize = 32;
        public Color bodyColor = Color.white;
        public Color keyTextColor = Color.white;

        [Header("Keycap")]
        public Vector2 keycapSize = new Vector2(0.58f, 0.62f);
        public Color keycapFill = new Color(0.01f, 0.04f, 0.06f, 0.72f);
        public Color keycapBorderColor = Color.white;
        [Range(0f, 0.2f)] public float borderThickness = 0.045f;

        [Header("Motion")]
        [Range(0f, 0.15f)] public float bobDistance = 0.03f;
        [Range(0f, 8f)] public float bobSpeed = 3f;
    }

    private Style style = new Style();
    private GameObject promptObject;
    private Vector3 baseLocalPosition;
    private bool shouldShow;
    private bool isInitialized;
    private static Sprite roundedFillSprite;
    private static Sprite roundedOutlineSprite;

    public void Configure(Style promptStyle)
    {
        style = promptStyle ?? new Style();
    }

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        CreatePrompt();
        SetVisible(shouldShow);
    }

    public void SetVisible(bool visible)
    {
        shouldShow = visible;
        if (promptObject != null)
        {
            promptObject.SetActive(visible);
        }
    }

    private void CreatePrompt()
    {
        TMP_FontAsset bodyFont = TMP_Settings.defaultFontAsset;
        if (bodyFont == null)
        {
            Debug.LogWarning("Boat boarding prompt could not find a usable font.", this);
            return;
        }

        promptObject = new GameObject("Board Boat Prompt");
        promptObject.transform.SetParent(transform, false);
        baseLocalPosition = style.localOffset;
        promptObject.transform.localPosition = baseLocalPosition;
        promptObject.transform.localScale = Vector3.one * style.worldScale;

        float keyCenterX = -0.1f;
        // Keep the text gap proportional to the keycap, whether it is configured
        // in small world units or the larger virtual units used in this scene.
        float labelGap = Mathf.Max(style.keycapSize.x * 0.7f, 0.12f);
        float beforeRightX = keyCenterX - style.keycapSize.x * 0.5f - labelGap;
        float afterLeftX = keyCenterX + style.keycapSize.x * 0.7f + labelGap;

        CreateKeycap("Keycap Fill", keyCenterX, style.keycapSize, style.keycapFill, roundedFillSprite ??= CreateRoundedSprite(false), style.sortingOrder);
        CreateKeycap("Outer Keycap Outline", keyCenterX, style.keycapSize + Vector2.one * style.borderThickness * 2f, style.keycapBorderColor, roundedOutlineSprite ??= CreateRoundedSprite(true), style.sortingOrder + 1);
        CreateKeycap("Inner Keycap Outline", keyCenterX, style.keycapSize - Vector2.one * style.borderThickness * 3f, style.keycapBorderColor, roundedOutlineSprite, style.sortingOrder + 2);
        CreateWorldText("Key Label", style.keyText, bodyFont, style.keyFontSize, new Vector3(keyCenterX, 0f, -0.03f), TextAlignmentOptions.Center, style.keyTextColor, style.sortingOrder + 4, new Vector2(0.8f, 0.8f));
        CreateWorldText("Before Key", style.beforeKeyText, bodyFont, style.bodyFontSize, new Vector3(beforeRightX, 0f, -0.03f), TextAlignmentOptions.MidlineRight, style.bodyColor, style.sortingOrder + 3, new Vector2(2.5f, 0.8f));
        CreateWorldText("After Key", style.afterKeyText, bodyFont, style.bodyFontSize, new Vector3(afterLeftX, 0f, -0.03f), TextAlignmentOptions.MidlineLeft, style.bodyColor, style.sortingOrder + 3, new Vector2(3.5f, 0.8f));
    }

    private void CreateKeycap(string objectName, float xPosition, Vector2 size, Color color, Sprite sprite, int sortingOrder)
    {
        GameObject keycap = new GameObject(objectName, typeof(SpriteRenderer));
        keycap.transform.SetParent(promptObject.transform, false);
        keycap.transform.localPosition = new Vector3(xPosition, 0f, 0f);
        keycap.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = keycap.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private void CreateWorldText(string objectName, string value, TMP_FontAsset font, int fontSize, Vector3 position, TextAlignmentOptions alignment, Color color, int sortingOrder, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshPro));
        textObject.transform.SetParent(promptObject.transform, false);
        textObject.transform.localPosition = position;
        textObject.transform.localScale = Vector3.one * WorldTextUnitScale;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = size;

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize * 0.1f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = color;

        MeshRenderer renderer = textObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = sortingOrder;
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
                bool drawPixel = insideOuter && (!outline || !insideInner);
                texture.SetPixel(x, y, drawPixel ? Color.white : Color.clear);
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
        float xDistance = localX - nearestX;
        float yDistance = localY - nearestY;
        return xDistance * xDistance + yDistance * yDistance <= radius * radius;
    }

    private void Update()
    {
        if (promptObject == null || !promptObject.activeSelf)
        {
            return;
        }

        float pulse = Mathf.Sin(Time.unscaledTime * style.bobSpeed) * style.bobDistance;
        promptObject.transform.localPosition = baseLocalPosition + Vector3.up * pulse;
    }
}
