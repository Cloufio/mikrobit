using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the two-page achievement carousel using the three existing card slots in AchievementScene.
/// The page art and unlock states are editable in the Inspector.
/// </summary>
public class AchievementPageController : MonoBehaviour
{
    [Header("Page One: The Fool, Chariot, Strength")]
    [SerializeField] private Sprite[] firstPageCards = new Sprite[3];
    [SerializeField] private bool[] firstPageUnlocked = { true, false, false };

    [Header("Page Two: Star, Moon, World")]
    [SerializeField] private Sprite[] secondPageCards = new Sprite[3];
    [SerializeField] private bool[] secondPageUnlocked = { false, false, false };

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private Color activeIndicatorColor = Color.white;
    [SerializeField] private Color inactiveIndicatorColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float unlockedOverlayAlpha = 0.31f;
    [SerializeField, Range(0f, 1f)] private float lockedOverlayAlpha = 0.99f;

    private readonly List<Image[]> cardLayers = new();
    private Button nextPageButton;
    private Button previousPageButton;
    private Image firstPageIndicator;
    private Image secondPageIndicator;
    private AchievementCardUnlockFX foolCardEffect;
    private int currentPage;

    private void Awake()
    {
        CacheSceneObjects();
        WirePageButtons();
        ShowPageImmediately(0);
    }

    public void NextPage()
    {
        ShowSecondPage();
    }

    public void PreviousPage()
    {
        ShowFirstPage();
    }

    public void ShowFirstPage()
    {
        ShowPage(0);
    }

    public void ShowSecondPage()
    {
        ShowPage(1);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowPage(int page)
    {
        if (page == currentPage)
        {
            return;
        }

        ApplyPageContent(page);
        currentPage = page;
        UpdatePageIndicators();
        UpdateNavigationButtons();
    }

    private void ShowPageImmediately(int page)
    {
        currentPage = page;
        ApplyPageContent(page);
        UpdatePageIndicators();
        UpdateNavigationButtons();
    }

    private void ApplyPageContent(int page)
    {
        Sprite[] sprites = page == 0 ? firstPageCards : secondPageCards;
        bool[] unlockedStates = page == 0 ? firstPageUnlocked : secondPageUnlocked;

        for (int i = 0; i < cardLayers.Count; i++)
        {
            if (i >= sprites.Length || sprites[i] == null)
            {
                continue;
            }

            Image[] layers = cardLayers[i];
            bool isUnlocked = i < unlockedStates.Length && unlockedStates[i];

            foreach (Image layer in layers)
            {
                layer.sprite = sprites[i];
            }

            if (layers.Length > 0)
            {
                layers[0].color = Color.white;
            }

            if (layers.Length > 1)
            {
                layers[1].color = new Color(0f, 0f, 0f, isUnlocked ? unlockedOverlayAlpha : lockedOverlayAlpha);
            }
        }

        if (foolCardEffect != null)
        {
            foolCardEffect.enabled = page == 0 && firstPageUnlocked.Length > 0 && firstPageUnlocked[0];
        }
    }

    private void UpdatePageIndicators()
    {
        if (firstPageIndicator != null)
        {
            firstPageIndicator.color = currentPage == 0 ? activeIndicatorColor : inactiveIndicatorColor;
        }

        if (secondPageIndicator != null)
        {
            secondPageIndicator.color = currentPage == 1 ? activeIndicatorColor : inactiveIndicatorColor;
        }
    }

    private void UpdateNavigationButtons()
    {
        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(currentPage == 0);
        }

        if (previousPageButton != null)
        {
            previousPageButton.gameObject.SetActive(currentPage == 1);
        }
    }

    private void CacheSceneObjects()
    {
        for (int i = 1; i <= 3; i++)
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
        }

        firstPageIndicator = FindChildObject("FirstPageButton")?.GetComponent<Image>();
        secondPageIndicator = FindChildObject("SecondPageButton")?.GetComponent<Image>();
        nextPageButton = FindChildObject("NextPageButton")?.GetComponent<Button>();
        previousPageButton = FindChildObject("PreviousButton")?.GetComponent<Button>();
        foolCardEffect = transform.Find("Card1")?.GetComponent<AchievementCardUnlockFX>();
    }

    private void WirePageButtons()
    {
        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(NextPage);
        }

        if (previousPageButton != null)
        {
            previousPageButton.onClick.AddListener(PreviousPage);
        }
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child.gameObject : null;
    }
}
