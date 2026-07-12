using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public int currentHealth = 5;
    public Image hudHealthBar;
    public SpriteRenderer worldHealthBar;

    private Vector3 worldHealthBarFullScale;

    void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth == 0 && maxHealth > 0)
        {
            currentHealth = maxHealth;
        }

        if (worldHealthBar != null)
        {
            worldHealthBarFullScale = worldHealthBar.transform.localScale;
        }
    }

    void Start()
    {
        UpdateHealthBars();
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthBars();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthBars();
    }

    public void UpdateHealthBars()
    {
        float healthPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        if (hudHealthBar != null)
        {
            hudHealthBar.type = Image.Type.Filled;
            hudHealthBar.fillMethod = Image.FillMethod.Horizontal;
            hudHealthBar.fillOrigin = 0;
            hudHealthBar.fillAmount = healthPercent;
        }

        if (worldHealthBar != null)
        {
            Vector3 scale = worldHealthBarFullScale;
            scale.x *= healthPercent;
            worldHealthBar.transform.localScale = scale;
        }
    }
}
