using UnityEngine;
using UnityEngine.UI;

public class TreeCut : Tool
{
    [Header("Tree Stats")]
    [SerializeField] int treeHealth = 25;
    [SerializeField] int damagePerHit = 10;

    [Header("Scoring")]
    [SerializeField] int pointsForCutting = 1;

    [Header("Cleanup Feedback")]
    [SerializeField] private bool useWaterCleanupFeedback = true;
    [SerializeField] private AudioClip cleanupSound;
    [Range(0f, 1f)] [SerializeField] private float cleanupSoundVolume = 0.8f;

    [Header("UI Visuals")]
    public Slider healthBarSlider;
    private int maxHealth;

    private void Awake()
    {
        // Trash remains selectable by ToolController while also physically blocking the boat.
        foreach (Collider2D interactionCollider in GetComponents<Collider2D>())
        {
            interactionCollider.isTrigger = false;
        }

        healthBarSlider = GetComponentInChildren<Slider>();

        if (healthBarSlider == null)
        {
            Debug.LogError(gameObject.name + " could not find a Slider in its children! Make sure the health bar is part of the prefab.");
        }
        else
        {
            // Set the slider's maximum and current value immediately
            maxHealth = treeHealth;
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = maxHealth;

            // Hide the health bar initially
            healthBarSlider.gameObject.SetActive(false);
        }
    }

    public override void Hit()
    {
        if (healthBarSlider != null && !healthBarSlider.gameObject.activeInHierarchy)
        {
            healthBarSlider.gameObject.SetActive(true);
        }

        treeHealth -= damagePerHit;
        Debug.Log(gameObject.name + " was hit! Remaining health: " + treeHealth);

        if (healthBarSlider != null)
        {
            // Feed the raw health number directly into the slider
            healthBarSlider.value = treeHealth;
        }

        if (treeHealth <= 0)
        {
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
}
