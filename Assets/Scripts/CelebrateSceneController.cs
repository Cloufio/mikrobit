using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// A self-contained end-of-run celebration. The score reveals from zero while
/// the three progress rings fill toward the 100-point cleanup target.
/// </summary>
[DisallowMultipleComponent]
public sealed class CelebrateSceneController : MonoBehaviour
{
    [Header("Preview")]
    [SerializeField, Min(0)] private int previewScore = 72;
    [SerializeField, Min(0.5f)] private float scoreRevealDuration = 2.25f;
    [SerializeField] private TMP_FontAsset boldPixelsFont;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private readonly List<Material> runtimeMaterials = new List<Material>();
    private readonly List<Image> rings = new List<Image>();
    private readonly List<Image> particles = new List<Image>();

    private readonly Color ink = new Color(0.03f, 0.08f, 0.16f, 1f);
    private readonly Color cream = new Color(0.96f, 0.98f, 0.94f, 1f);
    private readonly Color mint = new Color(0.25f, 0.95f, 0.68f, 1f);
    private readonly Color aqua = new Color(0.18f, 0.73f, 0.98f, 1f);
    private readonly Color sunshine = new Color(1f, 0.78f, 0.20f, 1f);

    private Sprite whiteSprite;
    private TMP_Text scoreLabel;
    private TMP_Text targetLabel;
    private TMP_Text praiseLabel;
    private RectTransform scoreCore;

    private void Start()
    {
        CreateInterface();
        StartCoroutine(PlayCelebration(GetRunScore()));
    }

    private int GetRunScore()
    {
        return PlayerPrefs.HasKey("Microbit_LastScore")
            ? Mathf.Max(0, PlayerPrefs.GetInt("Microbit_LastScore"))
            : Mathf.Max(0, previewScore);
    }

    private void CreateInterface()
    {
        whiteSprite = CreateSolidSprite();
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("Celebrate Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        CreateImage("Night Ocean", root, whiteSprite, new Color(0.015f, 0.04f, 0.10f, 1f), Vector2.zero, Vector2.one, Vector2.zero);
        CreateImage("Ocean Aura", root, whiteSprite, new Color(0.03f, 0.24f, 0.31f, 0.52f), new Vector2(0.24f, 0.50f), new Vector2(0.76f, 0.50f), new Vector2(0f, 780f));

        TMP_Text eyebrow = CreateText("Eyebrow", root, "SELESAI BERSIHKAN LAUT", 36f, mint, TextAlignmentOptions.Center);
        SetAnchors(eyebrow.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), new Vector2(0f, 72f));

        TMP_Text heading = CreateText("Heading", root, "JEJAK BAIKMU HARI INI", 62f, cream, TextAlignmentOptions.Center);
        SetAnchors(heading.rectTransform, new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), new Vector2(0f, 110f));

        RectTransform ringRoot = CreateRect("Progress Rings", root);
        SetAnchors(ringRoot, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(620f, 620f));
        AddRing(ringRoot, 620f, 44f, sunshine, 0.00f);
        AddRing(ringRoot, 500f, 40f, mint, 0.12f);
        AddRing(ringRoot, 390f, 34f, aqua, 0.24f);

        scoreCore = CreateImage("Score Core", ringRoot, whiteSprite, new Color(0.02f, 0.10f, 0.18f, 0.94f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(315f, 315f)).rectTransform;
        TMP_Text spark = CreateText("Spark", scoreCore, "✦", 48f, sunshine, TextAlignmentOptions.Center);
        SetAnchors(spark.rectTransform, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(90f, 64f));
        scoreLabel = CreateText("Score", scoreCore, "0", 124f, cream, TextAlignmentOptions.Center);
        SetAnchors(scoreLabel.rectTransform, new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), new Vector2(280f, 140f));
        targetLabel = CreateText("Score Target", scoreCore, "DARI TARGET 100", 25f, aqua, TextAlignmentOptions.Center);
        SetAnchors(targetLabel.rectTransform, new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.33f), new Vector2(280f, 48f));

        praiseLabel = CreateText("Praise", root, string.Empty, 38f, cream, TextAlignmentOptions.Center);
        praiseLabel.enableWordWrapping = true;
        SetAnchors(praiseLabel.rectTransform, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(1100f, 128f));

        Button menu = CreateButton("Menu Button", root, "KEMBALI KE MENU");
        SetAnchors(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.075f), new Vector2(0.5f, 0.075f), new Vector2(360f, 76f));
        menu.onClick.AddListener(ReturnToMenu);

        CreateParticles(root);
    }

    private void AddRing(RectTransform parent, float diameter, float thickness, Color color, float rotationOffset)
    {
        Sprite ringSprite = CreateRingSprite(384, thickness / diameter);
        Image track = CreateImage("Progress Ring Track", parent, ringSprite, new Color(0.12f, 0.23f, 0.29f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(diameter, diameter));
        track.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationOffset * 360f);
        Image ring = CreateImage("Progress Ring", parent, ringSprite, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(diameter, diameter));
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillOrigin = (int)Image.Origin360.Top;
        ring.fillClockwise = true;
        ring.fillAmount = 0f;
        ring.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotationOffset * 360f);
        rings.Add(ring);
    }

    private IEnumerator PlayCelebration(int targetScore)
    {
        int visualTarget = Mathf.Clamp(targetScore, 0, 100);
        float elapsed = 0f;
        while (elapsed < scoreRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / scoreRevealDuration));
            int shownScore = Mathf.RoundToInt(Mathf.Lerp(0f, targetScore, progress));
            scoreLabel.text = shownScore.ToString();
            targetLabel.text = shownScore >= 100 ? "TARGET 100 TERCAPAI!" : shownScore + " / 100 TARGET BERSIH";

            // The outer rings intentionally finish a fraction apart for a layered activity-ring feel.
            for (int i = 0; i < rings.Count; i++)
            {
                float delayedProgress = Mathf.Clamp01((progress - (i * 0.09f)) / (1f - (i * 0.09f)));
                rings[i].fillAmount = Mathf.Clamp01((visualTarget / 100f) * delayedProgress);
            }

            float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 7f) * 0.035f;
            scoreCore.localScale = Vector3.one * pulse;
            yield return null;
        }

        scoreLabel.text = targetScore.ToString();
        targetLabel.text = targetScore >= 100 ? "TARGET 100 TERCAPAI!" : targetScore + " / 100 TARGET BERSIH";
        for (int i = 0; i < rings.Count; i++)
            rings[i].fillAmount = visualTarget / 100f;

        StartCoroutine(AnimateParticles());
        yield return StartCoroutine(RevealPraise(GetPraise(targetScore)));
    }

    private string GetPraise(int score)
    {
        if (score >= 100)
            return "LUAR BIASA! Kamu melampaui target dan memberi laut kesempatan untuk bernapas lagi.";
        if (score >= 50)
            return "KEREN BANGET! Setiap sampah yang kamu angkat membuat ombak terasa lebih lega.";
        return "LANGKAH KECILMU BERARTI. Terima kasih sudah memulai perubahan untuk laut kita.";
    }

    private IEnumerator RevealPraise(string message)
    {
        praiseLabel.text = message;
        praiseLabel.maxVisibleCharacters = 0;
        praiseLabel.rectTransform.localScale = Vector3.one * 0.92f;
        CanvasGroup group = praiseLabel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        int length = message.Length;
        for (int visible = 0; visible <= length; visible++)
        {
            praiseLabel.maxVisibleCharacters = visible;
            group.alpha = Mathf.Clamp01(visible / 10f);
            praiseLabel.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, Mathf.Clamp01(visible / 15f));
            yield return new WaitForSecondsRealtime(0.018f);
        }

        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            praiseLabel.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.025f);
            yield return null;
        }
        praiseLabel.rectTransform.localScale = Vector3.one;
    }

    private void CreateParticles(RectTransform root)
    {
        Random.InitState(1024);
        for (int i = 0; i < 30; i++)
        {
            Color color = i % 3 == 0 ? sunshine : i % 3 == 1 ? mint : aqua;
            Image particle = CreateImage("Confetti", root, whiteSprite, color, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(12f, 12f));
            particle.rectTransform.anchoredPosition = Random.insideUnitCircle * 140f;
            particle.gameObject.SetActive(false);
            particles.Add(particle);
        }
    }

    private IEnumerator AnimateParticles()
    {
        foreach (Image particle in particles)
            particle.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < 1.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = elapsed / 1.3f;
            for (int i = 0; i < particles.Count; i++)
            {
                Image particle = particles[i];
                float angle = i * Mathf.PI * 2f / particles.Count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                particle.rectTransform.anchoredPosition = direction * Mathf.Lerp(120f, 470f, normalized) + Vector2.down * normalized * normalized * 170f;
                particle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, elapsed * (120f + i * 9f));
                particle.color = new Color(particle.color.r, particle.color.g, particle.color.b, 1f - normalized);
            }
            yield return null;
        }
    }

    private TMP_Text CreateText(string objectName, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = boldPixelsFont != null ? boldPixelsFont : TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = color;
        label.raycastTarget = false;
        StyleText(label, color);
        return label;
    }

    private void StyleText(TMP_Text label, Color color)
    {
        if (label.fontSharedMaterial == null)
            return;

        Material material = new Material(label.fontSharedMaterial);
        runtimeMaterials.Add(material);
        label.fontMaterial = material;
        SetMaterialColor(material, "_FaceColor", color);
        SetMaterialColor(material, "_OutlineColor", new Color(0f, 0.03f, 0.08f, 1f));
        SetMaterialColor(material, "_UnderlayColor", new Color(0f, 0f, 0f, 0.72f));
        SetMaterialFloat(material, "_OutlineWidth", 0.11f);
        SetMaterialFloat(material, "_UnderlayOffsetX", 0.12f);
        SetMaterialFloat(material, "_UnderlayOffsetY", -0.14f);
        SetMaterialFloat(material, "_UnderlayDilate", 0.05f);
    }

    private Button CreateButton(string objectName, Transform parent, string text)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.GetComponent<Image>();
        image.sprite = whiteSprite;
        image.color = new Color(0.04f, 0.20f, 0.26f, 0.95f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.72f, 1f, 0.91f, 1f);
        colors.pressedColor = new Color(0.36f, 0.78f, 0.67f, 1f);
        button.colors = colors;

        TMP_Text label = CreateText("Label", buttonObject.transform, text, 28f, mint, TextAlignmentOptions.Center);
        SetAnchors(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero);
        return button;
    }

    private Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        SetAnchors(image.rectTransform, anchorMin, anchorMax, size);
        return image;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject objectRoot = new GameObject(objectName, typeof(RectTransform));
        objectRoot.transform.SetParent(parent, false);
        return objectRoot.GetComponent<RectTransform>();
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    private static Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite CreateRingSprite(int resolution, float thicknessRatio)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        float outer = 0.49f;
        float inner = outer - thicknessRatio;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = (x + 0.5f) / resolution - 0.5f;
                float dy = (y + 0.5f) / resolution - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = distance <= outer && distance >= inner ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void SetMaterialColor(Material material, string property, Color color)
    {
        if (material.HasProperty(property))
            material.SetColor(property, color);
    }

    private static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
                Destroy(material);
        }
        runtimeMaterials.Clear();
    }
}
