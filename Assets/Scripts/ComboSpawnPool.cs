using UnityEngine;

/// <summary>
/// Supplies the existing TreeSpawnPoint objects with materials used by the
/// microplastic combos. Core ingredients are intentionally weighted higher so
/// players can discover recipes without excessive grinding.
/// </summary>
public sealed class ComboSpawnPool : MonoBehaviour
{
    private static ComboSpawnPool instance;

    [Header("Recipe Materials")]
    [SerializeField] private GameObject[] coreMaterials;
    [SerializeField] private GameObject[] supportingMaterials;

    [Header("Friendly Spawn Tuning")]
    [Range(0f, 1f)] [SerializeField] private float coreMaterialChance = 0.72f;
    [SerializeField] private bool replaceExistingSpawnPools = true;

    private void Awake()
    {
        instance = this;
    }

    public static bool TryGetSpawnPrefab(out GameObject prefab)
    {
        prefab = null;
        if (instance == null || !instance.replaceExistingSpawnPools)
        {
            return false;
        }

        GameObject[] preferredPool = Random.value < instance.coreMaterialChance
            ? instance.coreMaterials
            : instance.supportingMaterials;
        GameObject[] fallbackPool = preferredPool == instance.coreMaterials
            ? instance.supportingMaterials
            : instance.coreMaterials;

        prefab = Pick(preferredPool) ?? Pick(fallbackPool);
        return prefab != null;
    }

    private static GameObject Pick(GameObject[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        for (int attempt = 0; attempt < pool.Length; attempt++)
        {
            GameObject candidate = pool[Random.Range(0, pool.Length)];
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }
}
