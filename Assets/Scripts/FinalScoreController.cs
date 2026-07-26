using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class FinalScoreController : MonoBehaviour
{
    private const int MaximumVisibleRunAchievements = 10;

    [Header("Run Preview")]
    [SerializeField] private int previewScore = 130;
    [SerializeField] private int previewBestScore = 140;

    [Header("Tire Insight")]
    [SerializeField] private string trashName = "CAR TIRE";
    [SerializeField, Min(0)] private int tiresCleaned = 20;
    [SerializeField, TextArea(3, 5)] private string tireFunFact =
        "Car tires slowly wear away every time they touch the road. Rain can carry those tiny particles through drains and into rivers and seas.";
    [SerializeField] private TMP_FontAsset boldPixelsFont;

    [Header("Text Style")]
    [SerializeField] private Color titleColor = new Color(1f, 0.82f, 0.40f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.94f, 0.97f, 0.93f, 1f);
    [SerializeField] private Color outlineColor = new Color(0.24f, 0.12f, 0.13f, 1f);
    [SerializeField] private Color shadowColor = new Color(0.72f, 0.29f, 0.18f, 1f);

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string replaySceneName = "IntroScene";

    private readonly List<Material> runtimeMaterials = new List<Material>();

    private void Start()
    {
        ApplyBoardWidthRatio();
        ApplyExistingLayoutContent(GetRunScore(), GetBestScore());
        BuildRunAchievementPanel();
        ConnectNavigationButtons();
    }

    private void ApplyBoardWidthRatio()
    {
        Transform leftPanel = FindDeep(transform, "LeftPanel");
        Transform rightPanel = FindDeep(transform, "RightPanel");
        Transform mainLayout = FindDeep(transform, "MainLayout");
        if (leftPanel == null || rightPanel == null || mainLayout == null)
            return;

        // The HorizontalLayoutGroup was resolving both art panels to nearly the
        // same width. These fixed Canvas-space sizes make the 1:2 composition
        // deterministic at the project's 1920x1080 reference resolution.
        HorizontalLayoutGroup horizontalLayout = mainLayout.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayout != null)
            horizontalLayout.enabled = false;

        SetFixedBoardRect(leftPanel.GetComponent<RectTransform>(), new Vector2(672f, 900f), new Vector2(-580f, 0f));
        SetFixedBoardRect(rightPanel.GetComponent<RectTransform>(), new Vector2(1120f, 900f), new Vector2(356f, 0f));
    }

    private static void SetFixedBoardRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void ConnectNavigationButtons()
    {
        Button menuButton = FindSceneButton("MenuButton");
        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(ReturnToMainMenu);
        }
        else
        {
            Debug.LogWarning("FinalScoreController could not find MenuButton in the active scene.");
        }

        Button replayButton = FindSceneButton("ReplayButton");
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(Replay);
        }
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("FinalScoreController loading " + mainMenuSceneName + ".");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void Replay()
    {
        SceneManager.LoadScene(replaySceneName);
    }

    private static Button FindSceneButton(string objectName)
    {
        Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button.name == objectName)
                return button;
        }

        return null;
    }

    private void ApplyExistingLayoutContent(int runScore, int bestScoreValue)
    {
        TMP_Text congrats = FindText("CongratsText");
        TMP_Text currentScore = FindText("CurrentScoreText");
        TMP_Text bestScore = FindText("BestScoreText");
        TMP_Text mostPicked = FindText("MostPickedTrashText");
        TMP_Text funFact = FindText("FunFactText");
        Image trashImage = FindImage("TrashImage");

        ConfigureText(congrats, "SELAMAT!", 64f, bodyColor, TextAlignmentOptions.Center);
        ConfigureText(currentScore, runScore.ToString(), 128f, titleColor, TextAlignmentOptions.Center);
        ConfigureText(bestScore, "SKOR TERBAIK : " + bestScoreValue, 48f, bodyColor, TextAlignmentOptions.Center);

        // The old tire insight is replaced by the run-specific achievement gallery.
        if (mostPicked != null)
            mostPicked.gameObject.SetActive(false);
        if (funFact != null)
            funFact.gameObject.SetActive(false);
        if (trashImage != null)
            trashImage.gameObject.SetActive(false);

        Transform oldInsight = FindDeep(transform, "SlideTire");
        if (oldInsight != null)
            oldInsight.gameObject.SetActive(false);
    }

    private void BuildRunAchievementPanel()
    {
        Transform rightPanel = FindDeep(transform, "RightPanel");
        if (rightPanel == null)
        {
            Debug.LogWarning("FinalScoreController could not find RightPanel for the run achievement gallery.");
            return;
        }

        Transform oldGallery = rightPanel.Find("RunAchievementGallery");
        if (oldGallery != null)
            Destroy(oldGallery.gameObject);

        RectTransform gallery = CreateRect("RunAchievementGallery", rightPanel);
        Stretch(gallery, new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f));

        List<string> runAchievementIds = new List<string>(MicroplasticComboTracker.NewlyUnlockedThisRun);
        TMP_Text heading = CreateRuntimeText(
            "Heading",
            gallery,
            "Kamu Membuka " + runAchievementIds.Count + " Achievement",
            80f,
            Color.white,
            TextAlignmentOptions.Center);
        ApplyFunFactStyle(heading);
        SetRect(heading.rectTransform, new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.5f), new Vector2(850f, 70f), Vector2.zero);

        if (runAchievementIds.Count == 0)
        {
            return;
        }

        RectTransform grid = CreateRect("Achievement Card Grid", gallery);
        Stretch(grid, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.74f));
        Canvas.ForceUpdateCanvases();
        BuildAchievementCards(grid, runAchievementIds);
    }

    private void BuildAchievementCards(RectTransform grid, List<string> achievementIds)
    {
        const int columns = 5;
        const float horizontalGap = 16f;
        const float verticalGap = 20f;
        const float padding = 8f;
        int visibleCards = Mathf.Min(MaximumVisibleRunAchievements, achievementIds.Count);
        int rows = Mathf.CeilToInt(visibleCards / (float)columns);
        float availableWidth = Mathf.Max(1f, grid.rect.width - padding * 2f - horizontalGap * (columns - 1));
        float availableHeight = Mathf.Max(1f, grid.rect.height - padding * 2f - verticalGap * (rows - 1));
        float cardWidth = availableWidth / columns;
        float cardHeight = Mathf.Min(availableHeight / rows, cardWidth * 1.58f);

        for (int index = 0; index < visibleCards; index++)
        {
            int column = index % columns;
            int row = index / columns;
            RectTransform card = CreateRect("Achievement Card " + (index + 1), grid);
            card.anchorMin = new Vector2(0f, 1f);
            card.anchorMax = new Vector2(0f, 1f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(cardWidth, cardHeight);
            card.anchoredPosition = new Vector2(
                padding + cardWidth * 0.5f + column * (cardWidth + horizontalGap),
                -padding - cardHeight * 0.5f - row * (cardHeight + verticalGap));

            Image artwork = card.gameObject.AddComponent<Image>();
            artwork.sprite = MainSceneAchievementArtworkLibrary.GetArtwork(achievementIds[index]);
            artwork.color = artwork.sprite != null ? Color.white : new Color(0.06f, 0.10f, 0.14f, 0.95f);
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;

            if (artwork.sprite == null)
            {
                TMP_Text fallback = CreateRuntimeText("Achievement Name", card, GetAchievementTitle(achievementIds[index]), 20f, bodyColor, TextAlignmentOptions.Center);
                fallback.enableWordWrapping = true;
                Stretch(fallback.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            }
        }
    }

    private static string GetAchievementTitle(string id)
    {
        foreach (MicroplasticComboTracker.ComboDefinition definition in MicroplasticComboTracker.AllDefinitions)
        {
            if (string.Equals(definition.id, id, System.StringComparison.OrdinalIgnoreCase))
                return definition.title;
        }
        return "Achievement Baru";
    }

    private TMP_Text CreateRuntimeText(string objectName, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        ConfigureText(label, text, fontSize, color, alignment);
        return label;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject objectRoot = new GameObject(objectName, typeof(RectTransform));
        objectRoot.transform.SetParent(parent, false);
        return objectRoot.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private int GetRunScore()
    {
        return PlayerPrefs.HasKey("Microbit_LastScore")
            ? Mathf.Max(0, PlayerPrefs.GetInt("Microbit_LastScore"))
            : Mathf.Max(0, previewScore);
    }

    private int GetBestScore()
    {
        int runScore = GetRunScore();
        if (PlayerPrefs.HasKey("Microbit_BestScore"))
            return Mathf.Max(runScore, PlayerPrefs.GetInt("Microbit_BestScore"));

        return Mathf.Max(runScore, previewBestScore);
    }

    private void ConfigureText(TMP_Text label, string value, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        if (label == null)
            return;

        label.richText = true;
        label.text = value;
        label.fontSize = fontSize;
        label.enableAutoSizing = false;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Overflow;

        if (boldPixelsFont == null)
            return;

        label.font = boldPixelsFont;
        Material material = new Material(label.fontSharedMaterial);
        runtimeMaterials.Add(material);
        label.fontMaterial = material;
        SetMaterialColor(material, "_FaceColor", color);
        SetMaterialColor(material, "_OutlineColor", outlineColor);
        SetMaterialColor(material, "_UnderlayColor", shadowColor);
        SetMaterialFloat(material, "_OutlineWidth", 0.12f);
        SetMaterialFloat(material, "_UnderlayOffsetX", 0.18f);
        SetMaterialFloat(material, "_UnderlayOffsetY", -0.18f);
        SetMaterialFloat(material, "_UnderlayDilate", 0.04f);
        SetMaterialFloat(material, "_UnderlaySoftness", 0f);
    }

    private void ApplyFunFactStyle(TMP_Text label)
    {
        if (label == null || label.fontMaterial == null)
            return;

        Material material = label.fontMaterial;
        SetMaterialColor(material, "_FaceColor", Color.white);
        SetMaterialColor(material, "_OutlineColor", Color.black);
        SetMaterialColor(material, "_UnderlayColor", new Color(0f, 0f, 0f, 0.9f));
        SetMaterialFloat(material, "_OutlineWidth", 0.08f);
        SetMaterialFloat(material, "_UnderlayOffsetX", 0.16f);
        SetMaterialFloat(material, "_UnderlayOffsetY", -0.16f);
        SetMaterialFloat(material, "_UnderlayDilate", 0.04f);
    }

    private TMP_Text FindText(string objectName)
    {
        Transform target = FindDeep(transform, objectName);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform target = FindDeep(transform, objectName);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeep(root.GetChild(i), objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void SetMaterialColor(Material material, string property, Color color)
    {
        if (material != null && material.HasProperty(property))
            material.SetColor(property, color);
    }

    private static void SetMaterialFloat(Material material, string property, float value)
    {
        if (material != null && material.HasProperty(property))
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
