using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Keeps the achievement-card artwork available to the runtime unlock popup
/// while MainScene is loaded.
/// </summary>
public sealed class MainSceneAchievementArtworkLibrary : MonoBehaviour
{
    [Serializable]
    public sealed class ArtworkEntry
    {
        public string achievementId;
        public Sprite artwork;
    }

    [SerializeField] private ArtworkEntry[] artwork = Array.Empty<ArtworkEntry>();
    [SerializeField] private TMP_FontAsset boldPixelsFont;
    [SerializeField, Min(1f)] private float funFactCharactersPerSecond = 16f;

    private static MainSceneAchievementArtworkLibrary instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static Sprite GetArtwork(string achievementId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(achievementId))
        {
            return null;
        }

        foreach (ArtworkEntry entry in instance.artwork)
        {
            if (entry != null && string.Equals(entry.achievementId, achievementId, StringComparison.OrdinalIgnoreCase))
            {
                return entry.artwork;
            }
        }

        return null;
    }

    public static TMP_FontAsset GetBoldPixelsFont()
    {
        return instance != null ? instance.boldPixelsFont : null;
    }

    public static float GetFunFactCharactersPerSecond()
    {
        return instance != null ? instance.funFactCharactersPerSecond : 16f;
    }
}
