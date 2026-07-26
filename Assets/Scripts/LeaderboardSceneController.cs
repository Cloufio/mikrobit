using System;
using System.Collections.Generic;
using LootLocker.Requests;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Populates the existing LeaderboardScene layout with live LootLocker scores.
/// The scene keeps its authored background, panel, fonts, and rank-row styling.
/// </summary>
[DisallowMultipleComponent]
public class LeaderboardSceneController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Rows")]
    [SerializeField] private int visibleRows = 5;
    [SerializeField] private Color statusColor = new(1f, 0.88f, 0.60f, 1f);
    [SerializeField] private Color firstPlaceColor = new(1f, 0.76f, 0.25f, 1f);
    [SerializeField] private Color secondPlaceColor = new(0.84f, 0.9f, 0.96f, 1f);
    [SerializeField] private Color thirdPlaceColor = new(0.79f, 0.49f, 0.28f, 1f);
    [SerializeField] private Color otherPlaceColor = new(0.97f, 0.98f, 1f, 1f);

    [Header("Runtime Row Layout")]
    [Tooltip("Size of the live rank, player, and score text generated below the column headings.")]
    [SerializeField, Min(1f)] private float rowFontSize = 64f;
    [SerializeField, Min(1f)] private float rowHeight = 86f;
    [SerializeField, Min(1f)] private float firstRowBelowHeader = 95f;
    [SerializeField, Min(1f)] private float rankColumnOffset = 470f;
    [SerializeField] private Vector2 rankRowSize = new(180f, 72f);
    [SerializeField] private Vector2 playerRowSize = new(620f, 72f);
    [SerializeField] private Vector2 scoreRowSize = new(220f, 72f);

    [Header("Responsive Canvas")]
    [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
    [SerializeField, Range(0f, 1f)] private float widthHeightMatch = 0.5f;

    private TMP_Text titleText;
    private TMP_Text statusText;
    private TMP_Text playerHeaderText;
    private TMP_Text scoreHeaderText;
    private readonly List<TMP_Text> rankTexts = new();
    private readonly List<TMP_Text> playerTexts = new();
    private readonly List<TMP_Text> scoreTexts = new();

    private void Awake()
    {
        EnsureEventSystem();
        ConfigureResponsiveCanvas();
        CacheLayout();
        ConnectBackButton();
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackToMainMenu();
        }
    }

    public void Refresh()
    {
        SetStatus("CONNECTING...");
        ClearRows();
        LeaderboardManager.EnsureInstance().RequestLeaderboard(ShowScores);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ShowScores(bool success, LootLockerLeaderboardMember[] entries, string message)
    {
        if (!success)
        {
            SetStatus(message);
            return;
        }

        if (entries == null || entries.Length == 0)
        {
            SetStatus("NO SCORES YET - BE THE FIRST");
            return;
        }

        SetStatus(string.Empty);
        int count = Mathf.Min(Mathf.Min(visibleRows, entries.Length), Mathf.Min(rankTexts.Count, Mathf.Min(playerTexts.Count, scoreTexts.Count)));
        for (int index = 0; index < count; index++)
        {
            LootLockerLeaderboardMember entry = entries[index];
            rankTexts[index].text = FormatRank(entry.rank);
            playerTexts[index].text = GetPlayerName(entry);
            scoreTexts[index].text = entry.score.ToString();
            SetRowColor(index, GetRankColor(entry.rank));
        }
    }

    private void ConfigureResponsiveCanvas()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = widthHeightMatch;
        }

        // These are the scenic layers. Stretching them removes the side bars
        // while preserving the existing authored overlay and leaderboard rows.
        foreach (string backgroundName in new[] { "Bg1", "Bg2", "Bg3", "Bg4" })
        {
            Transform background = FindDescendant(transform, backgroundName);
            if (background == null)
            {
                continue;
            }

            RectTransform rect = background.GetComponent<RectTransform>();
            if (rect == null)
            {
                continue;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    private void CacheLayout()
    {
        titleText = FindText("LeaderboardText");
        if (titleText != null)
        {
            titleText.text = "LEADERBOARD";
        }

        playerHeaderText = FindTextWithValue("Player");
        scoreHeaderText = FindTextWithValue("Score");

        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in allTexts)
        {
            if (text == null || text == titleText || text.gameObject.name == "Text (TMP)")
            {
                continue;
            }

            RectTransform rect = text.rectTransform;
            Vector2 position = rect.anchoredPosition;
            // The authored "Player" and "Score" column headers sit above the
            // first row at y = -196. Only the five row labels begin below -250.
            if (position.y > -250f)
            {
                continue;
            }

            if (position.x < -200f)
            {
                rankTexts.Add(text);
            }
            else if (position.x > 200f)
            {
                scoreTexts.Add(text);
            }
            else
            {
                playerTexts.Add(text);
            }
        }

        SortRows(rankTexts);
        SortRows(playerTexts);
        SortRows(scoreTexts);
        EnsureRowLayout();
        statusText = CreateStatusText();
    }

    private void EnsureRowLayout()
    {
        // The scene's static labels are placeholders. Live rows are rebuilt from
        // the actual column-header positions so changing the layout in Unity does
        // not leave scores floating outside their Player and Score columns.
        foreach (TMP_Text text in rankTexts)
        {
            text.gameObject.SetActive(false);
        }

        foreach (TMP_Text text in playerTexts)
        {
            text.gameObject.SetActive(false);
        }

        foreach (TMP_Text text in scoreTexts)
        {
            text.gameObject.SetActive(false);
        }

        rankTexts.Clear();
        playerTexts.Clear();
        scoreTexts.Clear();

        Transform existingContainer = transform.Find("Runtime Leaderboard Rows");
        if (existingContainer != null)
        {
            Destroy(existingContainer.gameObject);
        }

        GameObject containerObject = new("Runtime Leaderboard Rows", typeof(RectTransform));
        Canvas canvas = GetComponentInParent<Canvas>();
        containerObject.transform.SetParent(
            canvas != null ? FixedAspectRatioCanvas.GetContentRoot(canvas) : transform,
            false);
        RectTransform container = containerObject.GetComponent<RectTransform>();
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.pivot = new Vector2(0.5f, 0.5f);
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;

        Vector2 playerHeaderCenter = GetHeaderCenter(playerHeaderText, container, new Vector2(-80f, 300f));
        Vector2 scoreHeaderCenter = GetHeaderCenter(scoreHeaderText, container, new Vector2(520f, 300f));
        float firstRowY = Mathf.Min(playerHeaderCenter.y, scoreHeaderCenter.y) - firstRowBelowHeader;
        float rankX = playerHeaderCenter.x - rankColumnOffset;

        for (int index = 0; index < visibleRows; index++)
        {
            float y = firstRowY - index * rowHeight;
            rankTexts.Add(CreateRowText(container, $"Rank {index + 1}", new Vector2(rankX, y), rankRowSize, TextAlignmentOptions.Center));
            playerTexts.Add(CreateRowText(container, $"Player {index + 1}", new Vector2(playerHeaderCenter.x, y), playerRowSize, TextAlignmentOptions.Center));
            scoreTexts.Add(CreateRowText(container, $"Score {index + 1}", new Vector2(scoreHeaderCenter.x, y), scoreRowSize, TextAlignmentOptions.Center));
        }
    }

    private TMP_Text CreateRowText(RectTransform parent, string objectName, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            text.font = titleText.font;
            text.fontSharedMaterial = titleText.fontSharedMaterial;
        }

        text.fontSize = rowFontSize;
        text.color = otherPlaceColor;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private void ConnectBackButton()
    {
        Transform backTransform = transform.Find("BackToMenu");
        if (backTransform == null)
        {
            return;
        }

        Button button = backTransform.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(BackToMainMenu);
    }

    private TMP_Text CreateStatusText()
    {
        GameObject statusObject = new("Connection Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Canvas canvas = GetComponentInParent<Canvas>();
        statusObject.transform.SetParent(
            canvas != null ? FixedAspectRatioCanvas.GetContentRoot(canvas) : transform,
            false);

        TMP_Text text = statusObject.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            text.font = titleText.font;
            text.fontSharedMaterial = titleText.fontSharedMaterial;
        }

        text.fontSize = 35f;
        text.color = statusColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -170f);
        rect.sizeDelta = new Vector2(1180f, 70f);
        return text;
    }

    private void ClearRows()
    {
        for (int index = 0; index < rankTexts.Count; index++)
        {
            rankTexts[index].text = string.Empty;
            rankTexts[index].color = otherPlaceColor;
        }

        foreach (TMP_Text text in playerTexts)
        {
            text.text = string.Empty;
            text.color = otherPlaceColor;
        }

        foreach (TMP_Text text in scoreTexts)
        {
            text.text = string.Empty;
            text.color = otherPlaceColor;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private TMP_Text FindText(string objectName)
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == objectName)
            {
                return text;
            }
        }

        return null;
    }

    private TMP_Text FindTextWithValue(string value)
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text != null && string.Equals(text.text.Trim(), value, StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }
        }

        return null;
    }

    private static Vector2 GetHeaderCenter(TMP_Text header, RectTransform container, Vector2 fallback)
    {
        if (header == null)
        {
            return fallback;
        }

        Vector3 worldCenter = header.rectTransform.TransformPoint(header.rectTransform.rect.center);
        Vector3 localCenter = container.InverseTransformPoint(worldCenter);
        return new Vector2(localCenter.x, localCenter.y);
    }

    private static void SortRows(List<TMP_Text> rows)
    {
        rows.Sort((first, second) => second.rectTransform.anchoredPosition.y.CompareTo(first.rectTransform.anchoredPosition.y));
    }

    private void SetRowColor(int index, Color color)
    {
        if (index < rankTexts.Count)
        {
            rankTexts[index].color = color;
        }

        if (index < playerTexts.Count)
        {
            playerTexts[index].color = color;
        }

        if (index < scoreTexts.Count)
        {
            scoreTexts[index].color = color;
        }
    }

    private Color GetRankColor(int rank)
    {
        return rank switch
        {
            1 => firstPlaceColor,
            2 => secondPlaceColor,
            3 => thirdPlaceColor,
            _ => otherPlaceColor
        };
    }

    private static string FormatRank(int rank)
    {
        return rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => Mathf.Max(1, rank).ToString()
        };
    }

    private static string GetPlayerName(LootLockerLeaderboardMember entry)
    {
        if (entry?.player != null && !string.IsNullOrWhiteSpace(entry.player.name))
        {
            return entry.player.name;
        }

        return string.IsNullOrWhiteSpace(entry?.metadata) ? "USERNAME" : entry.metadata;
    }

    private static Transform FindDescendant(Transform parent, string objectName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == objectName)
            {
                return child;
            }

            Transform nested = FindDescendant(child, objectName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
