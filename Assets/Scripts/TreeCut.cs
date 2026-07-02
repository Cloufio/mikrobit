using UnityEngine;
using UnityEngine.UI;

public class TreeCut : Tool
{
    [Header("Tree Stats")]
    [SerializeField] int treeHealth = 25;
    [SerializeField] int damagePerHit = 10;

    [Header("Scoring")]
    [SerializeField] int pointsForCutting = 1;

    [Header("UI Visuals")]
    public Slider healthBarSlider;
    private int maxHealth;

    private void Awake()
    {
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
            Destroy(gameObject);
        }
    }
}