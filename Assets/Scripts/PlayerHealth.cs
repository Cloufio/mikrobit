using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 5;
    public int currentHealth = 5;
    public Image hudHealthBar;
    public SpriteRenderer worldHealthBar;
    public Image[] heartImages;
    public Sprite remainingHeartSprite;
    public Sprite lostHeartSprite;
    public bool loseHeartsFromRight = true;
    public KeyCode debugDamageKey = KeyCode.O;
    [SerializeField] private float damageShakeDuration = 0.18f;
    [SerializeField] private float damageShakeDistance = 7f;

    private Vector3 worldHealthBarFullScale;
    private Vector2[] heartOriginalPositions;
    private Coroutine heartShakeCoroutine;

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

        CacheHeartPositions();
    }

    void Start()
    {
        UpdateHealthBars();
    }

    void Update()
    {
        if (Input.GetKeyDown(debugDamageKey))
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int damage)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthBars();

        if (currentHealth < previousHealth)
        {
            ShakeHeartRow();
        }
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

        for (int i = 0; heartImages != null && i < heartImages.Length; i++)
        {
            Image heartImage = heartImages[i];
            if (heartImage == null)
            {
                continue;
            }

            int healthSlot = loseHeartsFromRight ? i : heartImages.Length - 1 - i;
            bool hasHealth = healthSlot < currentHealth;
            Sprite heartSprite = hasHealth ? remainingHeartSprite : lostHeartSprite;

            if (heartSprite != null)
            {
                heartImage.enabled = true;
                heartImage.sprite = heartSprite;
            }
            else
            {
                heartImage.enabled = hasHealth;
            }
        }

        if (worldHealthBar != null)
        {
            Vector3 scale = worldHealthBarFullScale;
            scale.x *= healthPercent;
            worldHealthBar.transform.localScale = scale;
        }
    }

    private void CacheHeartPositions()
    {
        if (heartImages == null)
        {
            return;
        }

        heartOriginalPositions = new Vector2[heartImages.Length];
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                heartOriginalPositions[i] = heartImages[i].rectTransform.anchoredPosition;
            }
        }
    }

    private void ShakeHeartRow()
    {
        if (heartImages == null || heartImages.Length == 0)
        {
            return;
        }

        if (heartOriginalPositions == null || heartOriginalPositions.Length != heartImages.Length)
        {
            CacheHeartPositions();
        }

        if (heartShakeCoroutine != null)
        {
            StopCoroutine(heartShakeCoroutine);
            RestoreHeartPositions();
        }

        heartShakeCoroutine = StartCoroutine(ShakeHeartRowRoutine());
    }

    private IEnumerator ShakeHeartRowRoutine()
    {
        float elapsed = 0f;

        while (elapsed < damageShakeDuration)
        {
            float strength = 1f - elapsed / damageShakeDuration;
            Vector2 offset = Random.insideUnitCircle * damageShakeDistance * strength;
            SetHeartPositions(offset);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        RestoreHeartPositions();
        heartShakeCoroutine = null;
    }

    private void SetHeartPositions(Vector2 offset)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                heartImages[i].rectTransform.anchoredPosition = heartOriginalPositions[i] + offset;
            }
        }
    }

    private void RestoreHeartPositions()
    {
        if (heartOriginalPositions == null)
        {
            return;
        }

        SetHeartPositions(Vector2.zero);
    }
}
