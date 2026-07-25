using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class BadEndingController : MonoBehaviour
{
    // --- Constants ---
    private const string WATER_RESOURCE_NAME = "WaterDetail6";
    private const string TRASH_RESOURCE_FOLDER = "Trash";

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

    private void Start()
    {
        // 1. Perform scene flooding to remove land and unify water tiles
        FloodScene();

        // 2. Spawn floating trash on the flooded water
        SpawnTrash();

        if (fadePanel == null)
        {
            Debug.LogError("BadEndingController: Fade Panel is not assigned in the Inspector!");
            enabled = false;
            return;
        }

        // Ensure Time.timeScale is 1 when scene starts
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Debug.Log("BadEndingController: Reset Time.timeScale to 1.");
        }

        // Start fade sequence
        StartCoroutine(SceneSequenceCoroutine());
    }

    private void FloodScene()
    {
        // 1. Find all Tilemaps in the scene
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
        Tilemap waterTilemap = null;

        // 2. Identify water Tilemap and disable land Tilemaps
        foreach (Tilemap tm in tilemaps)
        {
            string tmName = tm.gameObject.name.ToLower();
            if (tmName.Contains("water"))
            {
                waterTilemap = tm;
            }
            else
            {
                tm.gameObject.SetActive(false);
            }
        }

        // 3. Disable all other land/floor/spawner/tree GameObjects in scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go == null || go == gameObject) continue;

            string name = go.name.ToLower();

            // Skip critical scene manager components, lights, cameras, canvas, and water
            if (IsCriticalObject(name)) continue;

            // Disable objects related to land, spawners, trees, etc.
            if (IsLandOrEnvironmentObject(name))
            {
                go.SetActive(false);
            }
        }

        // 4. Fill the water tilemap with gameplay water tile to represent a complete flood
        if (waterTilemap != null)
        {
            TileBase waterTile = Resources.Load<TileBase>(WATER_RESOURCE_NAME);
            if (waterTile != null)
            {
                waterTilemap.ClearAllTiles();

                const int gridSize = 60;
                for (int x = -gridSize; x <= gridSize; x++)
                {
                    for (int y = -gridSize; y <= gridSize; y++)
                    {
                        waterTilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
                    }
                }
                Debug.Log("BadEndingController: Successfully flooded scene with gameplay water.");
            }
            else
            {
                Debug.LogError($"BadEndingController: Could not find '{WATER_RESOURCE_NAME}' in Resources!");
            }
        }
        else
        {
            Debug.LogError("BadEndingController: Could not find Water Tilemap in the scene!");
        }
    }

    private bool IsCriticalObject(string name)
    {
        return name.Contains("manager") ||
               name.Contains("camera") ||
               name.Contains("light") ||
               name.Contains("canvas") ||
               name.Contains("fade") ||
               name.Contains("water");
    }

    private bool IsLandOrEnvironmentObject(string name)
    {
        return name.Contains("floor") ||
               name.Contains("spawn") ||
               name.Contains("map") ||
               name.Contains("tree") ||
               name.Contains("plant") ||
               name.Contains("forest") ||
               name.Contains("bridge") ||
               name.Contains("grass");
    }

    private void SpawnTrash()
    {
        GameObject[] trashPrefabs = Resources.LoadAll<GameObject>(TRASH_RESOURCE_FOLDER);
        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogWarning($"BadEndingController: No trash prefabs found in Resources/{TRASH_RESOURCE_FOLDER}!");
            return;
        }

        int trashCount = Random.Range(30, 50);
        for (int i = 0; i < trashCount; i++)
        {
            GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];

            float rx = Random.Range(-20f, 20f);
            float ry = Random.Range(-20f, 20f);
            Vector3 spawnPos = new Vector3(rx, ry, 0f);

            GameObject spawned = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            // Freeze rigidbodies to keep trash static
            Rigidbody2D rb = spawned.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        Debug.Log($"BadEndingController: Successfully spawned {trashCount} trash items on flooded water.");
    }

    private IEnumerator SceneSequenceCoroutine()
    {
        fadePanel.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration)); // Fade In

        Debug.Log($"BadEndingController: Fade-in complete. Waiting for {waitDuration} seconds.");
        yield return new WaitForSeconds(waitDuration);

        Debug.Log("BadEndingController: Wait complete. Starting fade-out.");
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration)); // Fade Out

        Debug.Log($"BadEndingController: Loading scene with build index {sceneIndexToLoad}.");
        SceneManager.LoadScene(sceneIndexToLoad);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color panelColor = fadePanel.color;

        fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, startAlpha);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, newAlpha);
            yield return null;
        }

        fadePanel.color = new Color(panelColor.r, panelColor.g, panelColor.b, endAlpha);
    }
}

