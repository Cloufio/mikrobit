using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class AchievementCardDefinition
{
    [SerializeField] private string id;
    [SerializeField] private string title;
    [SerializeField, TextArea(2, 4)] private string requirement;
    [SerializeField, TextArea(4, 8)] private string funFact;
    [SerializeField] private Sprite artwork;
    [SerializeField] private bool unlocked;

    public string Id => id;
    public string Title => title;
    public string Requirement => requirement;
    public string FunFact => funFact;
    public Sprite Artwork => artwork;
    public bool Unlocked => unlocked;
}

/// <summary>
/// Shows the ordered microplastic-combo card catalogue in the three existing card slots.
/// Combo detection can be added later; the unlock state is currently editable per card.
/// </summary>
public class AchievementPageController : MonoBehaviour
{
    private const int CardsPerPage = 3;
    private static readonly string[] PageIndicatorNames =
    {
        "FirstPageButton",
        "SecondPageButton",
        "ThirdPageButton",
        "FourthPageButton",
        "FifthPageButton"
    };

    [Header("Achievement Cards (Ordered)")]
    [SerializeField] private AchievementCardDefinition[] achievementCards = Array.Empty<AchievementCardDefinition>();

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Color activeIndicatorColor = Color.white;
    [SerializeField] private Color inactiveIndicatorColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float unlockedOverlayAlpha = 0.31f;
    [SerializeField, Range(0f, 1f)] private float lockedOverlayAlpha = 0.99f;

    private readonly List<Image[]> cardLayers = new();
    private readonly List<AchievementCardUnlockFX> cardEffects = new();
    private readonly List<Image> pageIndicators = new();
    private Button nextPageButton;
    private Button previousPageButton;
    private TMP_Text progressText;
    private int currentPage;

    private int PageCount => Mathf.Max(1, Mathf.CeilToInt(achievementCards.Length / (float)CardsPerPage));

    private void Awake()
    {
        CacheSceneObjects();
        ShowPageImmediately(0);
    }

    public void NextPage()
    {
        ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        ShowPage(currentPage - 1);
    }

    public void ShowFirstPage()
    {
        ShowPage(0);
    }

    public void ShowSecondPage()
    {
        ShowPage(Mathf.Min(1, PageCount - 1));
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowPage(int page)
    {
        int clampedPage = Mathf.Clamp(page, 0, PageCount - 1);
        if (clampedPage == currentPage)
        {
            return;
        }

        currentPage = clampedPage;
        ApplyPageContent();
        UpdatePageIndicators();
        UpdateNavigationButtons();
        UpdateProgressText();
    }

    private void ShowPageImmediately(int page)
    {
        currentPage = Mathf.Clamp(page, 0, PageCount - 1);
        ApplyPageContent();
        UpdatePageIndicators();
        UpdateNavigationButtons();
        UpdateProgressText();
    }

    private void ApplyPageContent()
    {
        int pageStart = currentPage * CardsPerPage;

        for (int slot = 0; slot < cardLayers.Count; slot++)
        {
            int cardIndex = pageStart + slot;
            AchievementCardDefinition card = cardIndex < achievementCards.Length ? achievementCards[cardIndex] : null;
            AchievementCardUnlockFX cardEffect = slot < cardEffects.Count ? cardEffects[slot] : null;

            // Disable before swapping art so the effect restores the previous card cleanly.
            if (cardEffect != null && cardEffect.enabled)
            {
                cardEffect.enabled = false;
            }

            ApplyCardToSlot(cardLayers[slot], card);

            if (cardEffect != null && card != null)
            {
                cardEffect.ConfigureDetails(card.Unlocked, card.Title, card.Requirement, card.FunFact);
                cardEffect.enabled = card.Unlocked && card.Artwork != null;
            }
        }
    }

    private void ApplyCardToSlot(Image[] layers, AchievementCardDefinition card)
    {
        bool hasArtwork = card != null && card.Artwork != null;

        foreach (Image layer in layers)
        {
            if (layer != null)
            {
                layer.enabled = hasArtwork;
                layer.sprite = hasArtwork ? card.Artwork : null;
            }
        }

        if (!hasArtwork || layers.Length == 0)
        {
            return;
        }

        layers[0].color = Color.white;

        if (layers.Length > 1 && layers[1] != null)
        {
            layers[1].color = new Color(0f, 0f, 0f, card.Unlocked ? unlockedOverlayAlpha : lockedOverlayAlpha);
        }
    }

    private void UpdatePageIndicators()
    {
        for (int index = 0; index < pageIndicators.Count; index++)
        {
            Image indicator = pageIndicators[index];
            if (indicator == null)
            {
                continue;
            }

            indicator.color = index == currentPage ? activeIndicatorColor : inactiveIndicatorColor;
        }
    }

    private void UpdateNavigationButtons()
    {
        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(currentPage < PageCount - 1);
        }

        if (previousPageButton != null)
        {
            previousPageButton.gameObject.SetActive(currentPage > 0);
        }
    }

    private void UpdateProgressText()
    {
        if (progressText == null)
        {
            return;
        }

        int unlockedCount = 0;
        foreach (AchievementCardDefinition card in achievementCards)
        {
            if (card != null && card.Unlocked)
            {
                unlockedCount++;
            }
        }

        progressText.text = $"Unlocked {unlockedCount}/{achievementCards.Length}";
    }

    private void CacheSceneObjects()
    {
        AchievementCardUnlockFX visualEffectTemplate = null;

        for (int i = 1; i <= CardsPerPage; i++)
        {
            Transform card = transform.Find($"Card{i}");
            if (card == null)
            {
                continue;
            }

            List<Image> directLayers = new();
            foreach (Transform child in card)
            {
                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    directLayers.Add(image);
                }
            }

            cardLayers.Add(directLayers.ToArray());

            AchievementCardUnlockFX cardEffect = card.GetComponent<AchievementCardUnlockFX>();
            if (cardEffect == null)
            {
                cardEffect = card.gameObject.AddComponent<AchievementCardUnlockFX>();
            }

            if (visualEffectTemplate == null)
            {
                visualEffectTemplate = cardEffect;
            }
            else
            {
                cardEffect.CopyVisualStyleFrom(visualEffectTemplate);
            }

            RectTransform[] layers = new RectTransform[directLayers.Count];
            for (int layerIndex = 0; layerIndex < directLayers.Count; layerIndex++)
            {
                layers[layerIndex] = directLayers[layerIndex].rectTransform;
            }

            cardEffect.SetCardLayers(layers);
            cardEffects.Add(cardEffect);
        }

        for (int index = 0; index < PageIndicatorNames.Length; index++)
        {
            GameObject pageIndicatorObject = FindChildObject(PageIndicatorNames[index]);
            Image pageIndicator = pageIndicatorObject?.GetComponent<Image>();
            if (pageIndicator == null)
            {
                continue;
            }

            pageIndicators.Add(pageIndicator);

            Button pageButton = pageIndicatorObject.GetComponent<Button>();
            if (pageButton != null)
            {
                int pageIndex = index;
                pageButton.onClick.AddListener(() => ShowPage(pageIndex));
            }
        }

        nextPageButton = FindChildObject("NextPageButton")?.GetComponent<Button>();
        previousPageButton = FindChildObject("PreviousButton")?.GetComponent<Button>();
        progressText = FindChildObject("AchievementUnlocked")?.GetComponent<TMP_Text>();
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child.gameObject : null;
    }
}
