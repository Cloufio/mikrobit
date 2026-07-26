using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker.Requests;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the leaderboard as a clipped, scrollable list while keeping the
/// column headers and the current player's position fixed in place.
/// </summary>
[DisallowMultipleComponent]
public class LeaderboardSceneController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Rows")]
    [SerializeField, Min(1)] private int visibleRows = 5;
    [SerializeField] private Color statusColor = new(1f, 0.88f, 0.60f, 1f);
    [SerializeField] private Color firstPlaceColor = new(1f, 0.76f, 0.25f, 1f);
    [SerializeField] private Color secondPlaceColor = new(0.84f, 0.9f, 0.96f, 1f);
    [SerializeField] private Color thirdPlaceColor = new(0.79f, 0.49f, 0.28f, 1f);
    [SerializeField] private Color otherPlaceColor = new(0.97f, 0.98f, 1f, 1f);
    [SerializeField] private Color currentPlayerColor = new(1f, 0.78f, 0.34f, 1f);
    [SerializeField] private Color currentPlayerBackground = new(0.035f, 0.12f, 0.21f, 0.88f);

    [Header("Runtime Row Layout")]
    [Tooltip("Size of the live rank, player, and score text generated below the column headings.")]
    [SerializeField, Min(1f)] private float rowFontSize = 64f;
    [SerializeField, Min(1f)] private float rowHeight = 86f;
    [SerializeField, Min(1f)] private float firstRowBelowHeader = 95f;
    [SerializeField, Min(0f)] private float playerRowGap = 32f;
    [SerializeField, Min(1f)] private float rankColumnOffset = 470f;
    [SerializeField, Min(1f)] private float scrollbarWidth = 18f;
    [SerializeField] private Vector2 rankRowSize = new(180f, 72f);
    [SerializeField] private Vector2 playerRowSize = new(620f, 72f);
    [SerializeField] private Vector2 scoreRowSize = new(220f, 72f);

    [Header("Editor Scroll Preview")]
    [Tooltip("Editor-only fake data for checking the scrollbar and clipping without needing many LootLocker accounts.")]
    [SerializeField] private bool useSampleRowsInEditor;
    [SerializeField, Min(6)] private int sampleRowCount = 12;

    [Header("Responsive Canvas")]
    [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
    [SerializeField, Range(0f, 1f)] private float widthHeightMatch = 0.5f;

    private TMP_Text titleText;
    private TMP_Text statusText;
    private TMP_Text playerHeaderText;
    private TMP_Text scoreHeaderText;
    private ScrollRect scoreScrollRect;
    private RectTransform scoreContent;
    private TMP_Text currentRankText;
    private TMP_Text currentPlayerText;
    private TMP_Text currentScoreText;
    private Vector2 rankPosition;
    private Vector2 playerPosition;
    private Vector2 scorePosition;
    private bool rowLayoutReady;
    private readonly List<TMP_Text> authoredRowTexts = new();

    private void Awake()
    {
        EnsureEventSystem();
        ConfigureResponsiveCanvas();
        CacheLayout();
        ConnectBackButton();
    }

    private IEnumerator Start()
    {
        // The global aspect presentation applies its camera viewport on the next
        // frame. Build the UI only after the final layout has settled.
        yield return null;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        EnsureRowLayout();
        statusText = CreateStatusText();
        rowLayoutReady = true;
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
        if (!rowLayoutReady)
        {
            return;
        }

#if UNITY_EDITOR
        if (useSampleRowsInEditor)
        {
            SetStatus("SAMPLE SCROLL PREVIEW");
            BuildSampleRows();
            SetCurrentPlayerRow("7", LeaderboardManager.EnsureInstance().CurrentPlayerName, "82", currentPlayerColor);
            return;
        }
#endif

        SetStatus("CONNECTING...");
        ClearRows();
        SetCurrentPlayerRow("—", LeaderboardManager.EnsureInstance().CurrentPlayerName, "—", currentPlayerColor);
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
            SetCurrentPlayerRow("—", LeaderboardManager.EnsureInstance().CurrentPlayerName, "—", currentPlayerColor);
            return;
        }

        if (entries == null || entries.Length == 0)
        {
            SetStatus("NO SCORES YET - BE THE FIRST");
        }
        else
        {
            SetStatus(string.Empty);
            BuildScoreRows(entries);
        }

        // This is intentionally a separate request. It returns the player's real
        // rank, even when they are outside the first page currently loaded above.
        LeaderboardManager.EnsureInstance().RequestCurrentPlayerRank(ShowCurrentPlayerPosition);
    }

    private void ShowCurrentPlayerPosition(bool success, int rank, int score, string message)
    {
        if (!success || rank <= 0)
        {
            SetCurrentPlayerRow("—", "BELUM ADA SKOR", "—", currentPlayerColor);
            return;
        }

        SetCurrentPlayerRow(FormatRank(rank), LeaderboardManager.EnsureInstance().CurrentPlayerName, score.ToString(), currentPlayerColor);
    }

    private void ConfigureResponsiveCanvas()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = widthHeightMatch;
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

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null || text == titleText || text == playerHeaderText || text == scoreHeaderText || text.gameObject.name == "Text (TMP)")
            {
                continue;
            }

            // Only disable the authored placeholder rows, never the title,
            // headers, or button label.
            if (text.rectTransform.anchoredPosition.y <= -250f)
            {
                authoredRowTexts.Add(text);
            }
        }
    }

    private void EnsureRowLayout()
    {
        foreach (TMP_Text text in authoredRowTexts)
        {
            text.gameObject.SetActive(false);
        }

        RemoveRuntimeObject("Runtime Leaderboard Scroll");
        RemoveRuntimeObject("Runtime Player Position");

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform root = canvas != null
            ? FixedAspectRatioCanvas.GetContentRoot(canvas) as RectTransform
            : transform as RectTransform;
        if (root == null)
        {
            return;
        }

        Vector2 playerHeaderCenter = GetHeaderCenter(playerHeaderText, root, new Vector2(-80f, 300f));
        Vector2 scoreHeaderCenter = GetHeaderCenter(scoreHeaderText, root, new Vector2(520f, 300f));
        float firstRowY = Mathf.Min(playerHeaderCenter.y, scoreHeaderCenter.y) - firstRowBelowHeader;
        float rankX = playerHeaderCenter.x - rankColumnOffset;

        float left = Mathf.Min(
            rankX - rankRowSize.x * 0.5f,
            Mathf.Min(playerHeaderCenter.x - playerRowSize.x * 0.5f, scoreHeaderCenter.x - scoreRowSize.x * 0.5f));
        float right = Mathf.Max(
            rankX + rankRowSize.x * 0.5f,
            Mathf.Max(playerHeaderCenter.x + playerRowSize.x * 0.5f, scoreHeaderCenter.x + scoreRowSize.x * 0.5f));
        float dataWidth = right - left;
        float dataCenterX = (left + right) * 0.5f;
        float viewportHeight = visibleRows * rowHeight;
        float viewportTop = firstRowY + rowHeight * 0.5f;
        float viewportCenterY = viewportTop - viewportHeight * 0.5f;
        float currentPlayerY = viewportTop - viewportHeight - playerRowGap - rowHeight * 0.5f;

        CreateScrollableRows(root, left, dataWidth, viewportCenterY, viewportHeight, dataCenterX, rankX, playerHeaderCenter.x, scoreHeaderCenter.x);
        CreateCurrentPlayerRow(root, dataCenterX, currentPlayerY, dataWidth, rankX, playerHeaderCenter.x, scoreHeaderCenter.x);
    }

    private void CreateScrollableRows(RectTransform root, float left, float dataWidth, float centerY, float height, float dataCenterX, float rankX, float playerX, float scoreX)
    {
        GameObject scrollObject = new("Runtime Leaderboard Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(root, false);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRectTransform.pivot = new Vector2(0f, 0.5f);
        scrollRectTransform.anchoredPosition = new Vector2(left, centerY);
        scrollRectTransform.sizeDelta = new Vector2(dataWidth + scrollbarWidth + 14f, height);

        Image scrollBackground = scrollObject.GetComponent<Image>();
        scrollBackground.color = new Color(1f, 1f, 1f, 0.001f);
        scrollBackground.raycastTarget = true;

        scoreScrollRect = scrollObject.GetComponent<ScrollRect>();
        scoreScrollRect.horizontal = false;
        scoreScrollRect.vertical = true;
        scoreScrollRect.movementType = ScrollRect.MovementType.Clamped;
        scoreScrollRect.scrollSensitivity = 45f;
        scoreScrollRect.inertia = true;

        GameObject viewportObject = new("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = viewport.anchorMax = new Vector2(0f, 0.5f);
        viewport.pivot = new Vector2(0f, 0.5f);
        viewport.anchoredPosition = Vector2.zero;
        viewport.sizeDelta = new Vector2(dataWidth, height);
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.001f);
        viewportImage.raycastTarget = true;

        GameObject contentObject = new("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObject.transform.SetParent(viewport, false);
        scoreContent = contentObject.GetComponent<RectTransform>();
        scoreContent.anchorMin = new Vector2(0f, 1f);
        scoreContent.anchorMax = new Vector2(1f, 1f);
        scoreContent.pivot = new Vector2(0.5f, 1f);
        scoreContent.anchoredPosition = Vector2.zero;
        scoreContent.sizeDelta = new Vector2(0f, height);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset();
        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        scoreScrollRect.viewport = viewport;
        scoreScrollRect.content = scoreContent;
        scoreScrollRect.verticalScrollbar = CreateScrollbar(scrollObject.transform, dataWidth, height);
        scoreScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Keep the text positions tied to the authored Player and Score headers.
        rankPosition = new Vector2(rankX - dataCenterX, 0f);
        playerPosition = new Vector2(playerX - dataCenterX, 0f);
        scorePosition = new Vector2(scoreX - dataCenterX, 0f);
    }

    private Scrollbar CreateScrollbar(Transform parent, float dataWidth, float height)
    {
        GameObject scrollbarObject = new("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(parent, false);
        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = scrollbarRect.anchorMax = new Vector2(0f, 0.5f);
        scrollbarRect.pivot = new Vector2(0f, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(dataWidth + 8f, 0f);
        scrollbarRect.sizeDelta = new Vector2(scrollbarWidth, height - 16f);
        Image track = scrollbarObject.GetComponent<Image>();
        track.sprite = GetUiSprite();
        track.color = new Color(0.02f, 0.06f, 0.11f, 0.55f);

        GameObject slidingAreaObject = new("Sliding Area", typeof(RectTransform));
        slidingAreaObject.transform.SetParent(scrollbarObject.transform, false);
        RectTransform slidingArea = slidingAreaObject.GetComponent<RectTransform>();
        slidingArea.anchorMin = Vector2.zero;
        slidingArea.anchorMax = Vector2.one;
        slidingArea.offsetMin = new Vector2(2f, 8f);
        slidingArea.offsetMax = new Vector2(-2f, -8f);

        GameObject handleObject = new("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(slidingArea, false);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        Image handleImage = handleObject.GetComponent<Image>();
        handleImage.sprite = GetUiSprite();
        handleImage.color = currentPlayerColor;

        Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        return scrollbar;
    }

    private void CreateCurrentPlayerRow(RectTransform root, float centerX, float centerY, float width, float rankX, float playerX, float scoreX)
    {
        GameObject rowObject = new("Runtime Player Position", typeof(RectTransform), typeof(Image));
        rowObject.transform.SetParent(root, false);
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(centerX, centerY);
        row.sizeDelta = new Vector2(width, rowHeight);
        Image background = rowObject.GetComponent<Image>();
        background.sprite = GetUiSprite();
        background.color = currentPlayerBackground;
        background.raycastTarget = false;

        currentRankText = CreateRowText(row, "Your Rank", new Vector2(rankX - centerX, 0f), rankRowSize);
        currentPlayerText = CreateRowText(row, "Your Name", new Vector2(playerX - centerX, 0f), playerRowSize);
        currentScoreText = CreateRowText(row, "Your Score", new Vector2(scoreX - centerX, 0f), scoreRowSize);
        SetCurrentPlayerRow("—", LeaderboardManager.EnsureInstance().CurrentPlayerName, "—", currentPlayerColor);
    }

    private void BuildScoreRows(IReadOnlyList<LootLockerLeaderboardMember> entries)
    {
        ClearRows();
        if (scoreContent == null)
        {
            return;
        }

        int count = entries.Count;
        scoreContent.sizeDelta = new Vector2(0f, Mathf.Max(visibleRows * rowHeight, count * rowHeight));

        for (int index = 0; index < count; index++)
        {
            LootLockerLeaderboardMember entry = entries[index];
            RectTransform row = CreateRow(scoreContent, $"Leaderboard Row {index + 1}");
            TMP_Text rank = CreateRowText(row, "Rank", rankPosition, rankRowSize);
            TMP_Text player = CreateRowText(row, "Player", playerPosition, playerRowSize);
            TMP_Text score = CreateRowText(row, "Score", scorePosition, scoreRowSize);
            Color color = GetRankColor(entry.rank);
            rank.text = FormatRank(entry.rank);
            player.text = GetPlayerName(entry);
            score.text = entry.score.ToString();
            rank.color = player.color = score.color = color;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(scoreContent);
        if (scoreScrollRect != null)
        {
            scoreScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void BuildSampleRows()
    {
        ClearRows();
        if (scoreContent == null)
        {
            return;
        }

        scoreContent.sizeDelta = new Vector2(0f, Mathf.Max(visibleRows * rowHeight, sampleRowCount * rowHeight));
        for (int index = 0; index < sampleRowCount; index++)
        {
            int rankNumber = index + 1;
            RectTransform row = CreateRow(scoreContent, $"Preview Row {rankNumber}");
            TMP_Text rank = CreateRowText(row, "Rank", rankPosition, rankRowSize);
            TMP_Text player = CreateRowText(row, "Player", playerPosition, playerRowSize);
            TMP_Text score = CreateRowText(row, "Score", scorePosition, scoreRowSize);
            rank.text = FormatRank(rankNumber);
            player.text = rankNumber == 7 ? LeaderboardManager.EnsureInstance().CurrentPlayerName : $"Preview Player {rankNumber}";
            score.text = Mathf.Max(4, 100 - index * 7).ToString();
            Color color = rankNumber == 7 ? currentPlayerColor : GetRankColor(rankNumber);
            rank.color = player.color = score.color = color;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(scoreContent);
        if (scoreScrollRect != null)
        {
            scoreScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ClearRows()
    {
        if (scoreContent == null)
        {
            return;
        }

        for (int index = scoreContent.childCount - 1; index >= 0; index--)
        {
            GameObject row = scoreContent.GetChild(index).gameObject;
            row.SetActive(false);
            Destroy(row);
        }
    }

    private RectTransform CreateRow(RectTransform parent, string objectName)
    {
        GameObject rowObject = new(objectName, typeof(RectTransform), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);
        RectTransform row = rowObject.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = rowHeight;
        layoutElement.preferredHeight = rowHeight;
        layoutElement.flexibleHeight = 0f;
        return row;
    }

    private TMP_Text CreateRowText(RectTransform parent, string objectName, Vector2 position, Vector2 size)
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
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private void SetCurrentPlayerRow(string rank, string player, string score, Color color)
    {
        if (currentRankText == null || currentPlayerText == null || currentScoreText == null)
        {
            return;
        }

        currentRankText.text = rank;
        currentPlayerText.text = player;
        currentScoreText.text = score;
        currentRankText.color = currentPlayerText.color = currentScoreText.color = color;
    }

    private void ConnectBackButton()
    {
        Transform backTransform = transform.Find("BackToMenu");
        if (backTransform == null || !backTransform.TryGetComponent(out Button button))
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(BackToMainMenu);
    }

    private TMP_Text CreateStatusText()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? FixedAspectRatioCanvas.GetContentRoot(canvas) : transform;
        GameObject statusObject = new("Connection Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(parent, false);
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
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -170f);
        rect.sizeDelta = new Vector2(1180f, 70f);
        return text;
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

    private static Vector2 GetHeaderCenter(TMP_Text header, RectTransform root, Vector2 fallback)
    {
        if (header == null)
        {
            return fallback;
        }

        Vector3 worldCenter = header.rectTransform.TransformPoint(header.rectTransform.rect.center);
        Vector3 localCenter = root.InverseTransformPoint(worldCenter);
        return new Vector2(localCenter.x, localCenter.y);
    }

    private static void RemoveRuntimeObject(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Destroy(existing);
        }
    }

    private Sprite GetUiSprite()
    {
        Transform board = transform.Find("Board");
        return board != null && board.TryGetComponent(out Image boardImage) ? boardImage.sprite : null;
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

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }

}
