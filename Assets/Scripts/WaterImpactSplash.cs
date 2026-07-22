using System.Collections.Generic;
using UnityEngine;

public class WaterImpactSplash : MonoBehaviour
{
    private const float Lifetime = 0.35f;
    private const float WakeLifetime = 0.52f;
    private const int WakeSortingOrder = 3;
    private static Sprite pixelSprite;

    private readonly List<SplashFragment> fragments = new List<SplashFragment>();
    private float elapsed;
    private bool isWake;
    private Vector2 wakeHeading;

    public static void Spawn(Vector2 position)
    {
        GameObject splashObject = new GameObject("Water Impact Splash");
        splashObject.transform.position = position;
        splashObject.AddComponent<WaterImpactSplash>();
    }

    public static void SpawnWake(Vector2 position, Vector2 heading)
    {
        GameObject wakeObject = new GameObject("Boat Wake");
        wakeObject.transform.position = position;

        WaterImpactSplash wake = wakeObject.AddComponent<WaterImpactSplash>();
        wake.isWake = true;
        wake.wakeHeading = heading.normalized;
    }

    private void Start()
    {
        if (isWake)
        {
            CreateWakeFragments();
            return;
        }

        const int fragmentCount = 7;
        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragmentObject = new GameObject("Splash Pixel");
            fragmentObject.transform.SetParent(transform, false);

            SpriteRenderer spriteRenderer = fragmentObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetPixelSprite();
            spriteRenderer.sortingOrder = 10;
            spriteRenderer.color = i % 2 == 0
                ? new Color(0.82f, 0.96f, 1f, 1f)
                : new Color(0.35f, 0.8f, 1f, 1f);

            float size = Random.Range(0.06f, 0.11f);
            fragmentObject.transform.localScale = Vector3.one * size;

            Vector2 velocity = GetFragmentVelocity();
            fragments.Add(new SplashFragment
            {
                Renderer = spriteRenderer,
                Velocity = velocity,
                InitialColor = spriteRenderer.color
            });
        }
    }

    private void CreateWakeFragments()
    {
        Vector2 heading = wakeHeading.sqrMagnitude > 0.001f ? wakeHeading.normalized : Vector2.up;
        Vector2 side = new Vector2(-heading.y, heading.x);
        float headingAngle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg;

        // Two diverging foam lanes make the wake legible while remaining below the boat.
        for (int laneIndex = -1; laneIndex <= 1; laneIndex += 2)
        {
            for (int segment = 0; segment < 3; segment++)
            {
                float distanceBehindBoat = 0.10f + segment * 0.18f;
                float laneDistance = 0.10f + segment * 0.07f;
                float lateralJitter = Random.Range(-0.025f, 0.025f);

                Vector2 startPosition = -heading * distanceBehindBoat
                    + side * (laneIndex * laneDistance + lateralJitter);
                Vector2 velocity = -heading * Random.Range(0.23f, 0.36f)
                    + side * laneIndex * Random.Range(0.08f, 0.16f);

                CreateWakeFragment(
                    startPosition,
                    velocity,
                    headingAngle + Random.Range(-10f, 10f),
                    Random.Range(0.18f, 0.30f),
                    Random.Range(0.025f, 0.040f),
                    segment == 0
                        ? new Color(0.92f, 0.99f, 1f, 0.9f)
                        : new Color(0.68f, 0.91f, 1f, 0.78f));
            }
        }

        // A small centre disturbance keeps the trail connected to the boat's stern.
        CreateWakeFragment(
            -heading * 0.12f,
            -heading * 0.28f,
            headingAngle,
            0.22f,
            0.035f,
            new Color(0.9f, 0.98f, 1f, 0.85f));
    }

    private void CreateWakeFragment(
        Vector2 localPosition,
        Vector2 velocity,
        float rotation,
        float width,
        float height,
        Color color)
    {
        GameObject fragmentObject = new GameObject("Wake Foam");
        fragmentObject.transform.SetParent(transform, false);
        fragmentObject.transform.localPosition = localPosition;
        fragmentObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        fragmentObject.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer spriteRenderer = fragmentObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetPixelSprite();
        spriteRenderer.sortingOrder = WakeSortingOrder;
        spriteRenderer.color = color;

        fragments.Add(new SplashFragment
        {
            Renderer = spriteRenderer,
            Velocity = velocity,
            InitialScale = fragmentObject.transform.localScale,
            InitialColor = color
        });
    }

    private Vector2 GetFragmentVelocity()
    {
        if (!isWake)
        {
            return Random.insideUnitCircle.normalized * Random.Range(0.8f, 1.4f) + Vector2.up * 0.35f;
        }

        Vector2 side = new Vector2(-wakeHeading.y, wakeHeading.x);
        float sidewaysSpeed = Random.Range(-0.65f, 0.65f);
        return -wakeHeading * Random.Range(0.15f, 0.35f) + side * sidewaysSpeed;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float lifetime = isWake ? WakeLifetime : Lifetime;
        float progress = elapsed / lifetime;

        foreach (SplashFragment fragment in fragments)
        {
            if (fragment.Renderer == null)
            {
                continue;
            }

            fragment.Renderer.transform.localPosition += (Vector3)(fragment.Velocity * Time.deltaTime);
            if (isWake)
            {
                float spread = 1f + progress * 0.55f;
                fragment.Renderer.transform.localScale = new Vector3(
                    fragment.InitialScale.x * spread,
                    fragment.InitialScale.y * (1f + progress * 0.25f),
                    1f);
            }

            Color color = fragment.InitialColor;
            color.a *= 1f - progress;
            fragment.Renderer.color = color;
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public static Sprite GetPixelSprite()
    {
        if (pixelSprite != null)
        {
            return pixelSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return pixelSprite;
    }

    private class SplashFragment
    {
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public Vector3 InitialScale;
        public Color InitialColor;
    }
}
