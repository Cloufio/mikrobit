using UnityEngine;
using UnityEngine.UI;

public class TreeCut : Tool
{
    [Header("Tree Stats")]
    [SerializeField] int treeHealth = 30;
    [SerializeField] int damagePerHit = 10;

    [Header("Scoring")]
    [SerializeField] int pointsForCutting = 1;

    [Header("Cleanup Feedback")]
    [SerializeField] private bool useWaterCleanupFeedback = true;
    [SerializeField] private AudioClip cleanupSound;
    [Range(0f, 1f)] [SerializeField] private float cleanupSoundVolume = 0.8f;

    private TrashPollutionPatch pollutionPatch;
    private Slider healthBarSlider;
    private int maxHealth;

    private void Awake()
    {
        // Every water-trash prefab uses the same three-click cleanup rule.
        if (ShouldUsePollutionPatch())
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

        if (ShouldUsePollutionPatch())
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
