using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Renamed Button Labels")]
    [SerializeField] private Color labelCoverColor = new Color(0.83f, 0.42f, 0.28f, 1f);
    [SerializeField] private Color labelColor = new Color(1f, 0.91f, 0.67f, 1f);
    [SerializeField] private float renamedButtonFontSize = 27f;

    private void Awake()
    {
        ConfigureRenamedButton("AchievementsButton", "Achievements");
        ConfigureRenamedButton("LeaderboardsButton", "Leaderboards");
    }

    public void playGame()
    {
        SceneManager.LoadScene("IntroScene");
    }

    public void ShowAchievements()
    {
        Debug.Log("Achievements selected. Create and connect an achievements page when it is ready.");
    }

    public void ShowLeaderboards()
    {
        Debug.Log("Leaderboards selected. Connect this button to your leaderboard service when it is ready.");
    }

    private void ConfigureRenamedButton(string buttonName, string label)
    {
        Transform buttonTransform = transform.Find(buttonName);
        if (buttonTransform == null)
        {
            Debug.LogWarning($"MainMenu could not find '{buttonName}'.", this);
            return;
        }

        RectTransform buttonRect = buttonTransform.GetComponent<RectTransform>();
        TMP_Text labelText = buttonTransform.GetComponentInChildren<TMP_Text>(true);
        if (buttonRect == null || labelText == null)
        {
            Debug.LogWarning($"MainMenu button '{buttonName}' is missing its UI components.", this);
            return;
        }

        Image coverImage = GetOrCreateLabelCover(buttonRect);
        RectTransform coverRect = coverImage.rectTransform;
        coverRect.anchorMin = new Vector2(0.5f, 0.5f);
        coverRect.anchorMax = new Vector2(0.5f, 0.5f);
        coverRect.pivot = new Vector2(0.5f, 0.5f);
        coverRect.anchoredPosition = Vector2.zero;
        coverRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x - 32f, buttonRect.sizeDelta.y - 34f);
        coverImage.color = labelCoverColor;

        labelText.gameObject.SetActive(true);
        labelText.transform.SetAsLastSibling();
        labelText.text = label;
        labelText.fontSize = renamedButtonFontSize;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = labelColor;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Overflow;

        RectTransform labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 0f);
        labelRect.offsetMax = new Vector2(-20f, 0f);
    }

    private static Image GetOrCreateLabelCover(RectTransform buttonRect)
    {
        Transform coverTransform = buttonRect.Find("Label Cover");
        if (coverTransform != null)
        {
            return coverTransform.GetComponent<Image>();
        }

        GameObject coverObject = new GameObject("Label Cover", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        coverObject.transform.SetParent(buttonRect, false);
        coverObject.transform.SetAsFirstSibling();

        Image coverImage = coverObject.GetComponent<Image>();
        coverImage.raycastTarget = false;
        return coverImage;
    }
}
