using UnityEngine;
using UnityEngine.UI;

public class TreeCut : Tool
{
    [Header("Tree Stats")]
    [SerializeField] int treeHealth = 30;
    [SerializeField] int damagePerHit = 10;

    [Header("Scoring")]
    [SerializeField] int pointsForCutting = 2;
    [SerializeField, Min(0f)] private float timeRewardOnCleanup = 0.3f;

    [Header("Cleanup Feedback")]
    [SerializeField] private bool useWaterCleanupFeedback = true;
    [SerializeField] private AudioClip cleanupSound;
    [Range(0f, 1f)] [SerializeField] private float cleanupSoundVolume = 0.8f;

    private TrashPollutionPatch pollutionPatch;
    private Slider healthBarSlider;
    private int maxHealth;
    private bool isWaterTrash;

    private void Awake()
    {
        // An inactive copy is kept only as a safe respawn template. It must not
        // create collision, pollution, or another respawn request of its own.
        if (WaterTrashRespawnManager.IsCreatingTemplate)
        {
            return;
        }

        isWaterTrash = ShouldUsePollutionPatch();

        // Every water-trash prefab uses the same three-click cleanup rule.
        if (isWaterTrash)
        {
            treeHealth = 30;
            damagePerHit = 10;
        }

        // Trash remains selectable by ToolController while also physically blocking the boat.
        foreach (Collider2D interactionCollider in GetComponents<Collider2D>())
        {
            interactionCollider.isTrigger = false;
        }

        healthBarSlider = GetComponentInChildren<Slider>(true);
        if (healthBarSlider != null)
        {
            maxHealth = treeHealth;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = maxHealth;
            healthBarSlider.gameObject.SetActive(false);
        }

        if (isWaterTrash)
        {
            pollutionPatch = TrashPollutionPatch.Spawn(transform);
        }
    }

    public override void Hit()
    {
        if (healthBarSlider != null && !healthBarSlider.gameObject.activeInHierarchy)
        {
            healthBarSlider.gameObject.SetActive(true);
        }

        treeHealth -= damagePerHit;
        if (healthBarSlider != null)
        {
            healthBarSlider.value = Mathf.Max(0, treeHealth);
        }

        if (treeHealth <= 0)
        {
            pollutionPatch?.Clean();
            MicroplasticComboTracker.RecordTrashCollected(gameObject.name);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(pointsForCutting);

                // Only water-trash objects grant time. Trees share this script
                // for their hit behaviour, but are not part of the cleanup goal.
                if (isWaterTrash)
                {
                    ScoreManager.Instance.AddTime(timeRewardOnCleanup);
                }
            }

            if (isWaterTrash)
            {
                WaterTrashRespawnManager.EnsureInstance().QueueRespawn(
                    gameObject,
                    transform.position,
                    transform.rotation);
            }

            if (useWaterCleanupFeedback)
            {
                WaterCleaningRipple.Spawn(transform.position);
            }

            CleanupScorePopup.Spawn(transform.position, pointsForCutting);

            if (cleanupSound != null)
            {
                AudioSource.PlayClipAtPoint(cleanupSound, transform.position, cleanupSoundVolume);
            }

            Destroy(gameObject);
        }
    }

    private bool ShouldUsePollutionPatch()
    {
        if (!useWaterCleanupFeedback)
        {
            return false;
        }

        // Water trash renders at order 2. Trees share this interaction script but
        // render at order 3, so they should not create pollution in the water.
        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        return rootRenderer != null && rootRenderer.sortingOrder <= 2;
    }
}
