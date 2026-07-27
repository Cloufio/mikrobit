using UnityEngine;

/// <summary>
/// A lightweight procedural patch of murky water beneath one water-trash object.
/// When the trash is cleaned, it turns clear blue and fades away.
/// </summary>
public class TrashPollutionPatch : MonoBehaviour
{
    private const int TextureWidth = 20;
    private const int TextureHeight = 12;
    private const float PixelsPerUnit = 8f;
    private const float CleanColorDuration = 0.24f;
    private const float CleanFadeDuration = 0.56f;

    private static readonly Color MurkyColor = new Color(0.42f, 0.29f, 0.11f, 0.62f);
    private static readonly Color CleanColor = new Color(0.28f, 0.78f, 0.95f, 0.7f);
    private static Sprite pollutionSprite;

    private SpriteRenderer patchRenderer;
    private Transform trashOwner;
    private Tool ownerTool;
    private Vector3 baseScale;
    private Vector3 centerPosition;
    private float animationPhase;
    private float animationSpeed;
    private float cleanElapsed;
    private bool isCleaning;

    public static TrashPollutionPatch Spawn(Transform owner)
    {
        if (owner == null)
        {
            return null;
        }

        GameObject patchObject = new GameObject(owner.name + " Pollution Patch");
        patchObject.transform.position = owner.position;

        TrashPollutionPatch patch = patchObject.AddComponent<TrashPollutionPatch>();
        patch.Initialize(owner);
        return patch;
    }

    /// <summary>
    /// Lets the murky water act as a forgiving click target for its trash.
    /// The patch is only spawned for water trash, so land objects are not affected.
    /// </summary>
    public Tool OwnerTool => !isCleaning ? ownerTool : null;

    public void Clean()
    {
        if (isCleaning)
        {
            return;
        }

        isCleaning = true;
        cleanElapsed = 0f;
        trashOwner = null;
    }

    private void Initialize(Transform owner)
    {
        trashOwner = owner;
        ownerTool = owner.GetComponent<Tool>();
        centerPosition = owner.position;
        animationPhase = Random.Range(0f, Mathf.PI * 2f);
        animationSpeed = Random.Range(0.8f, 1.2f);

        patchRenderer = gameObject.AddComponent<SpriteRenderer>();
        patchRenderer.sprite = GetPollutionSprite();
        patchRenderer.sortingOrder = 1;
        patchRenderer.color = MurkyColor;

        // Match the clickable area to the visible murky patch. This means a
        // player can clean trash by clicking its polluted water, not only the
        // small trash sprite in the middle.
        BoxCollider2D clickArea = gameObject.AddComponent<BoxCollider2D>();
        clickArea.isTrigger = true;
        clickArea.size = new Vector2(TextureWidth / PixelsPerUnit, TextureHeight / PixelsPerUnit);

        SpriteRenderer trashRenderer = owner.GetComponent<SpriteRenderer>();
        if (trashRenderer != null)
        {
            patchRenderer.sortingLayerID = trashRenderer.sortingLayerID;
        }

        float trashSize = trashRenderer != null
            ? Mathf.Max(trashRenderer.bounds.size.x, trashRenderer.bounds.size.y)
            : 1f;
        float patchDiameter = Mathf.Clamp(trashSize * 2.25f, 1.8f, 3.4f);
        float spriteWidth = Mathf.Max(pollutionSprite.bounds.size.x, 0.01f);
        float scale = patchDiameter / spriteWidth;

        baseScale = new Vector3(
            scale * Random.Range(0.92f, 1.08f),
            scale * Random.Range(0.86f, 0.98f),
            1f);
        transform.localScale = baseScale;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));
    }

    private void Update()
    {
        if (!isCleaning && trashOwner == null)
        {
            Clean();
        }

        if (isCleaning)
        {
            UpdateCleaningAnimation();
            return;
        }

        centerPosition = trashOwner.position;
        float time = Time.time * animationSpeed + animationPhase;
        float pulse = 1f + Mathf.Sin(time * 1.7f) * 0.025f;
        transform.localScale = baseScale * pulse;
        transform.position = centerPosition + new Vector3(
            Mathf.Sin(time * 0.7f) * 0.025f,
            Mathf.Cos(time * 0.55f) * 0.015f,
            0f);

        Color murkyPulse = MurkyColor;
        murkyPulse.a *= 0.92f + Mathf.Sin(time * 1.3f) * 0.08f;
        patchRenderer.color = murkyPulse;
    }

    private void UpdateCleaningAnimation()
    {
        cleanElapsed += Time.deltaTime;
        float totalDuration = CleanColorDuration + CleanFadeDuration;
        float colorProgress = Mathf.Clamp01(cleanElapsed / CleanColorDuration);
        float fadeProgress = Mathf.Clamp01((cleanElapsed - CleanColorDuration) / CleanFadeDuration);

        Color color = Color.Lerp(MurkyColor, CleanColor, colorProgress);
        color.a *= 1f - fadeProgress;
        patchRenderer.color = color;

        float expansion = 1f + Mathf.SmoothStep(0f, 0.2f, cleanElapsed / totalDuration);
        transform.localScale = baseScale * expansion;

        if (cleanElapsed >= totalDuration)
        {
            Destroy(gameObject);
        }
    }

    private static Sprite GetPollutionSprite()
    {
        if (pollutionSprite != null)
        {
            return pollutionSprite;
        }

        Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, true)
        {
            name = "Procedural Trash Pollution",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[TextureWidth * TextureHeight];
        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                float normalizedX = (x + 0.5f - TextureWidth * 0.5f) / (TextureWidth * 0.5f);
                float normalizedY = (y + 0.5f - TextureHeight * 0.5f) / (TextureHeight * 0.5f);
                float distance = normalizedX * normalizedX + normalizedY * normalizedY;
                float noise = Hash(x, y);
                float irregularEdge = 0.78f + noise * 0.24f;

                pixels[y * TextureWidth + x] = distance <= irregularEdge
                    ? new Color(1f, 1f, 1f, noise > 0.84f ? 0.42f : 1f)
                    : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        pollutionSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureWidth, TextureHeight),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
        pollutionSprite.name = "Procedural Trash Pollution";
        pollutionSprite.hideFlags = HideFlags.HideAndDontSave;
        return pollutionSprite;
    }

    private static float Hash(int x, int y)
    {
        uint value = (uint)(x * 374761393 + y * 668265263);
        value = (value ^ (value >> 13)) * 1274126177u;
        return (value & 0x00ffffffu) / 16777215f;
    }
}
