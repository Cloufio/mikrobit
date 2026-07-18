using System.Collections.Generic;
using UnityEngine;

public class WaterImpactSplash : MonoBehaviour
{
    private const float Lifetime = 0.35f;
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
        int fragmentCount = isWake ? 6 : 7;
        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragmentObject = new GameObject("Splash Pixel");
            fragmentObject.transform.SetParent(transform, false);

            SpriteRenderer spriteRenderer = fragmentObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetPixelSprite();
            spriteRenderer.sortingOrder = isWake ? 1 : 10;
            spriteRenderer.color = i % 2 == 0
                ? new Color(0.82f, 0.96f, 1f, 1f)
                : new Color(0.35f, 0.8f, 1f, 1f);

            float size = isWake ? Random.Range(0.055f, 0.1f) : Random.Range(0.06f, 0.11f);
            fragmentObject.transform.localScale = Vector3.one * size;

            Vector2 velocity = GetFragmentVelocity();
            fragments.Add(new SplashFragment
            {
                Renderer = spriteRenderer,
                Velocity = velocity
            });
        }
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
        float progress = elapsed / Lifetime;

        foreach (SplashFragment fragment in fragments)
        {
            if (fragment.Renderer == null)
            {
                continue;
            }

            fragment.Renderer.transform.localPosition += (Vector3)(fragment.Velocity * Time.deltaTime);
            Color color = fragment.Renderer.color;
            color.a = 1f - progress;
            fragment.Renderer.color = color;
        }

        if (elapsed >= Lifetime)
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
    }
}
