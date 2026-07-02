using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float damagePerHit = 10f;
    public float damageCooldown = 1f;

    [Header("UI Hearts Configuration")]
    [Tooltip("The 5 Heart Image components in the Canvas UI.")]
    public Image[] heartImages = new Image[5];

    [Header("Heart Sprite States")]
    [Tooltip("Sprite for a fully filled heart (4/4).")]
    public Sprite spriteFull;
    [Tooltip("Sprite for a 3/4 filled heart.")]
    public Sprite sprite3Quarters;
    [Tooltip("Sprite for a half-filled heart (2/4).")]
    public Sprite spriteHalf;
    [Tooltip("Sprite for a 1/4 filled heart.")]
    public Sprite sprite1Quarter;
    [Tooltip("Sprite for an empty heart (0/4).")]
    public Sprite spriteEmpty;

    private float cooldownTimer = 0f;

    private void Awake()
    {
        // Setup Singleton Instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentHealth = maxHealth;
    }

    private void Start()
    {
        UpdateHeartUI();
    }

    private void Update()
    {
        // Handle damage cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        // Don't take damage if in cooldown (invulnerable)
        if (cooldownTimer > 0)
        {
            return;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        cooldownTimer = damageCooldown;

        Debug.Log($"Player took {damageAmount} damage! Current Health: {currentHealth}/{maxHealth}");

        UpdateHeartUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void UpdateHeartUI()
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            Debug.LogWarning("PlayerHealth: Heart images array is not assigned or empty!");
            return;
        }

        // Each of the 5 hearts has 4 quarters, so there are 20 total steps/segments of health
        float healthPercentage = currentHealth / maxHealth;
        int totalQuartersRemaining = Mathf.RoundToInt(healthPercentage * 20f);

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            // Determine how many quarters of this specific heart are filled (0 to 4)
            int quartersForThisHeart = Mathf.Clamp(totalQuartersRemaining - (i * 4), 0, 4);

            switch (quartersForThisHeart)
            {
                case 4:
                    heartImages[i].sprite = spriteFull;
                    heartImages[i].enabled = true;
                    break;
                case 3:
                    heartImages[i].sprite = sprite3Quarters;
                    heartImages[i].enabled = true;
                    break;
                case 2:
                    heartImages[i].sprite = spriteHalf;
                    heartImages[i].enabled = true;
                    break;
                case 1:
                    heartImages[i].sprite = sprite1Quarter;
                    heartImages[i].enabled = true;
                    break;
                case 0:
                default:
                    heartImages[i].sprite = spriteEmpty;
                    // If no empty heart sprite is assigned, we can disable the image. Otherwise keep it.
                    if (spriteEmpty == null)
                    {
                        heartImages[i].enabled = false;
                    }
                    else
                    {
                        heartImages[i].enabled = true;
                    }
                    break;
            }
        }
    }

    private void Die()
    {
        Debug.Log("Player health reached 0. Triggering game over!");
        GameEndManager gameEndManager = FindObjectOfType<GameEndManager>();
        if (gameEndManager != null)
        {
            gameEndManager.TriggerGameOver();
        }
        else
        {
            Debug.LogError("PlayerHealth: GameEndManager not found in scene! Cannot trigger game over.");
        }
    }

    // Handles collision on foot (when player is walking and bumps into obstacles)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If the player is driving the boat, they shouldn't take damage directly via foot collisions
        // (the boat handles boat collisions, and player collider is disabled anyway, but good to be safe)
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Boat"))
        {
            return;
        }

        // Ignore trash (which contains a Tool component)
        if (collision.gameObject.GetComponent<Tool>() != null)
        {
            return;
        }

        // Apply damage for colliding with obstacles
        TakeDamage(damagePerHit);
    }
}
