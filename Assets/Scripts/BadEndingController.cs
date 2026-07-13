using UnityEngine;
using UnityEngine.UI;         // Required for UI Image
using UnityEngine.SceneManagement; // Required for scene management
using System.Collections;     // Required for Coroutines
using UnityEngine.Tilemaps;   // Required for Tilemaps

public class BadEndingSceneController : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("The UI Image to use for fading. It should cover the screen.")]
    public Image fadePanel;
    [Tooltip("How long the fade-in effect should take in seconds.")]
    public float fadeInDuration = 1.5f;
    [Tooltip("How long the fade-out effect should take in seconds.")]
    public float fadeOutDuration = 1.5f;
    [Tooltip("How long to wait (in seconds) after fade-in before starting fade-out.")]
    public float waitDuration = 2.0f;

    [Header("Scene Transition")]
    [Tooltip("The build index of the scene to load after fade-out (e.g., 0 for Main Menu).")]
    public int sceneIndexToLoad = 0;

    void Start()
    {
        // 1. Perform scene flooding to remove land and unify water tiles
        FloodScene();

        // 2. Spawn floating trash on the flooded water
        SpawnTrash();

        if (fadePanel == null)
        {
            Debug.LogError("BadEndingSceneController: Fade Panel is not assigned in the Inspector!");
            // Optionally, try to find it if not assigned, or disable the script
            // For simplicity, we'll just log an error. The scene won't fade correctly.
            enabled = false;
            return;
        }

        // Ensure Time.timeScale is 1 when this scene starts, in case it was set to 0 previously.
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Debug.Log("BadEndingSceneController: Time.timeScale was not 1, resetting to 1.");
        }

        // Start the sequence
        StartCoroutine(SceneSequenceCoroutine());
    }

    void FloodScene()
    {
        // 1. Find all Tilemaps in the scene
        var tilemaps = FindObjectsOfType<Tilemap>();
        Tilemap waterTilemap = null;

        // 2. Identify the water Tilemap and disable land Tilemaps
        foreach (var tm in tilemaps)
        {
            string name = tm.gameObject.name.ToLower();
            if (name.Contains("water"))
            {
                waterTilemap = tm;
            }
            else
            {
                tm.gameObject.SetActive(false);
            }
        }

        // 3. Disable all other land/floor/spawner/tree GameObjects in the scene
        var allObjects = FindObjectsOfType<GameObject>();
        foreach (var go in allObjects)
        {
            if (go == null) continue;

            string name = go.name.ToLower();

            // Skip critical scene manager components, lights, cameras, and UI canvas elements
            if (go == gameObject || 
                name.Contains("manager") || 
                name.Contains("camera") || 
                name.Contains("light") || 
                name.Contains("canvas") || 
                name.Contains("fade") || 
                name.Contains("water"))
            {
                continue;
            }

            // Disable objects related to land, spawners, trees, etc.
            if (name.Contains("floor") || 
                name.Contains("spawn") || 
                name.Contains("map") || 
                name.Contains("tree") || 
                name.Contains("plant") || 
                name.Contains("forest") ||
                name.Contains("bridge") ||
                name.Contains("grass"))
            {
                go.SetActive(false);
            }
        }

        // 4. Fill the water tilemap with the gameplay water tile to represent a complete flood
        if (waterTilemap != null)
        {
            var waterTile = Resources.Load<TileBase>("WaterDetail6");
            if (waterTile != null)
            {
                waterTilemap.ClearAllTiles();

                // Define a large grid size to cover the entire screen (e.g. -60 to 60)
                int size = 60;
                for (int x = -size; x <= size; x++)
                {
                    for (int y = -size; y <= size; y++)
                    {
                        waterTilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
                    }
                }
                Debug.Log("BadEndingSceneController: Successfully flooded scene with gameplay water.");
            }
            else
            {
                Debug.LogError("BadEndingSceneController: Could not find WaterDetail6 in Resources!");
            }
        }
        else
        {
            Debug.LogError("BadEndingSceneController: Could not find Water Tilemap in the scene!");
        }
    }

    void SpawnTrash()
    {
        // Load all trash prefabs from Resources/Trash
        GameObject[] trashPrefabs = Resources.LoadAll<GameObject>("Trash");
        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogWarning("BadEndingSceneController: No trash prefabs found in Resources/Trash!");
            return;
        }

        // Spawn a random number of trash items at random positions in the scene to represent the flood
        int trashCount = Random.Range(30, 50); // spawn between 30 and 50 trash items
        for (int i = 0; i < trashCount; i++)
        {
            GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];

            // Random position in range -20 to 20
            float rx = Random.Range(-20f, 20f);
            float ry = Random.Range(-20f, 20f);
            Vector3 spawnPos = new Vector3(rx, ry, 0f);

            GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            // Make sure the trash doesn't fall or get pushed away by physics
            var rb = spawned.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        Debug.Log($"BadEndingSceneController: Successfully spawned {trashCount} trash items on the flooded water.");
    }

    IEnumerator SceneSequenceCoroutine()
    {
        // 1. Start with the panel fully opaque (if it's not already, e.g., from a previous scene's fade-out)
        //    and then fade it in (alpha from 1 to 0).
        fadePanel.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration)); // Fade In

        // 2. Wait for the specified duration
        Debug.Log($"BadEndingSceneController: Fade-in complete. Waiting for {waitDuration} seconds.");
        yield return new WaitForSeconds(waitDuration);

        // 3. Fade out (alpha from 0 to 1)
        Debug.Log("BadEndingSceneController: Wait complete. Starting fade-out.");
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration)); // Fade Out

        // 4. Load the next scene
        Debug.Log($"BadEndingSceneController: Fade-out complete. Loading scene with build index {sceneIndexToLoad}.");
        SceneManager.LoadScene(sceneIndexToLoad);
    }

    IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color panelColor = fadePanel.color; // Get the base color (e.g., black)

        // Ensure the panel starts at the correct alpha
        fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, startAlpha);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime; // Use unscaled time if you want fade during Time.timeScale = 0, but usually not needed here
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, newAlpha);
            yield return null; // Wait for the next frame
        }

        // Ensure the panel is exactly at the endAlpha
        fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, endAlpha);
    }
}
