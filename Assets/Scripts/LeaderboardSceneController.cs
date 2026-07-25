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
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private int visibleRows = 5;
    [SerializeField] private Color statusColor = new(1f, 0.88f, 0.60f, 1f);

    private TMP_Text titleText;
    private TMP_Text statusText;
    private readonly List<TMP_Text> rankTexts = new();
    private readonly List<TMP_Text> playerTexts = new();
    private readonly List<TMP_Text> scoreTexts = new();

    private void Awake()
    {
        EnsureEventSystem();
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
            SetStatus("NO SCORES YET - BE THE FIRST CLEANER");
            return;
        }

        SetStatus("TOP OCEAN CLEANERS");
        int count = Mathf.Min(Mathf.Min(visibleRows, entries.Length), Mathf.Min(rankTexts.Count, Mathf.Min(playerTexts.Count, scoreTexts.Count)));
        for (int index = 0; index < count; index++)
        {
            LootLockerLeaderboardMember entry = entries[index];
            rankTexts[index].text = $"{entry.rank}.";
            playerTexts[index].text = string.IsNullOrWhiteSpace(entry.metadata) ? "OCEAN CLEANER" : entry.metadata;
            scoreTexts[index].text = entry.score.ToString();
        }
    }

    private void CacheLayout()
    {
        titleText = FindText("LeaderboardText");
        if (titleText != null)
        {
            titleText.text = "LEADERBOARD";
        }

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
        statusText = CreateStatusText();
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
        statusObject.transform.SetParent(transform, false);

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
        foreach (TMP_Text text in rankTexts)
        {
            text.text = string.Empty;
        }

        foreach (TMP_Text text in playerTexts)
        {
            text.text = string.Empty;
        }

        foreach (TMP_Text text in scoreTexts)
        {
            text.text = string.Empty;
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

    private static void SortRows(List<TMP_Text> rows)
    {
        rows.Sort((first, second) => second.rectTransform.anchoredPosition.y.CompareTo(first.rectTransform.anchoredPosition.y));
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
