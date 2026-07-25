using UnityEngine;
using UnityEngine.UI;
using LootLocker.Requests;
using System.Collections;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [Header("LootLocker Config")]
    [Tooltip("ID Leaderboard dari dashboard LootLocker.")]
    public int leaderboardID = 12345; 

    [Header("UI References (Optional)")]
    [Tooltip("UI Text atau TextMeshPro untuk menampilkan leaderboard.")]
    public Text leaderboardText;

    private bool isLoggedIn = false;
    private string playerID;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartGuestSession();
    }

    public void StartGuestSession()
    {
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                Debug.Log("LootLocker: Berhasil Login Guest!");
                playerID = response.player_id.ToString();
                isLoggedIn = true;
            }
            else
            {
                Debug.LogError("LootLocker: Gagal Login - " + (response.errorData != null ? response.errorData.message : "Unknown error"));
            }
        });
    }

    public void SubmitScore(int scoreToSubmit)
    {
        if (!isLoggedIn)
        {
            Debug.LogWarning("LootLocker: Belum terhubung, mencoba submit saat sesi siap...");
            StartCoroutine(SubmitScoreWhenLoggedIn(scoreToSubmit));
            return;
        }

        LootLockerSDKManager.SubmitScore(playerID, scoreToSubmit, leaderboardID, (response) =>
        {
            if (response.success)
            {
                Debug.Log($"LootLocker: Skor {scoreToSubmit} berhasil dikirim!");
            }
            else
            {
                Debug.LogError("LootLocker: Gagal submit skor - " + (response.errorData != null ? response.errorData.message : "Unknown error"));
            }
        });
    }

    private IEnumerator SubmitScoreWhenLoggedIn(int scoreToSubmit)
    {
        yield return new WaitUntil(() => isLoggedIn);
        SubmitScore(scoreToSubmit);
    }

    public void FetchTopScores()
    {
        int count = 10;

        LootLockerSDKManager.GetScoreList(leaderboardID, count, 0, (response) =>
        {
            if (response.success)
            {
                string formattedLeaderboard = "=== GLOBAL LEADERBOARD ===\n\n";
                LootLockerLeaderboardMember[] members = response.items;

                if (members != null)
                {
                    for (int i = 0; i < members.Length; i++)
                    {
                        formattedLeaderboard += $"{members[i].rank}. Player {members[i].player.id} : {members[i].score} pts\n";
                    }
                }

                Debug.Log(formattedLeaderboard);

                if (leaderboardText != null)
                {
                    leaderboardText.text = formattedLeaderboard;
                }
            }
            else
            {
                Debug.LogError("LootLocker: Gagal mengambil leaderboard - " + (response.errorData != null ? response.errorData.message : "Unknown error"));
            }
        });
    }

    // ==========================================
    // FITUR ACHIEVEMENT (PLAYER STORAGE CLOUD)
    // ==========================================

    /// <summary>
    /// Membuka (Unlock) Achievement di Cloud LootLocker
    /// Contoh penggunaan: LeaderboardManager.Instance.UnlockAchievement("GOOD_ENDING_UNLOCKED");
    /// </summary>
    public void UnlockAchievement(string achievementKey)
    {
        if (!isLoggedIn)
        {
            StartCoroutine(UnlockAchievementWhenLoggedIn(achievementKey));
            return;
        }

        LootLockerSDKManager.UpdateSingleKeyPersistentStorage(achievementKey, "unlocked", (response) =>
        {
            if (response.success)
            {
                Debug.Log($"LootLocker Achievement: '{achievementKey}' BERHASIL Di-unlock!");
            }
            else
            {
                Debug.LogError($"LootLocker Achievement: Gagal unlock '{achievementKey}' - " + (response.errorData != null ? response.errorData.message : "Unknown error"));
            }
        });
    }

    private IEnumerator UnlockAchievementWhenLoggedIn(string achievementKey)
    {
        yield return new WaitUntil(() => isLoggedIn);
        UnlockAchievement(achievementKey);
    }

    /// <summary>
    /// Mengecek apakah Achievement tertentu sudah di-unlock sebelumnya oleh player di Cloud
    /// </summary>
    public void CheckAchievement(string achievementKey, System.Action<bool> callback)
    {
        if (!isLoggedIn)
        {
            StartCoroutine(CheckAchievementWhenLoggedIn(achievementKey, callback));
            return;
        }

        LootLockerSDKManager.GetSingleKeyPersistentStorage(achievementKey, (response) =>
        {
            if (response.success && response.payload != null && response.payload.value == "unlocked")
            {
                callback?.Invoke(true);
            }
            else
            {
                callback?.Invoke(false);
            }
        });
    }

    private IEnumerator CheckAchievementWhenLoggedIn(string achievementKey, System.Action<bool> callback)
    {
        yield return new WaitUntil(() => isLoggedIn);
        CheckAchievement(achievementKey, callback);
    }
}
