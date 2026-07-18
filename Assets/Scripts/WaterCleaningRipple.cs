using System.Collections.Generic;
using UnityEngine;

public class WaterCleaningRipple : MonoBehaviour
{
    private const float Lifetime = 0.55f;
    private const int SegmentCount = 14;

    private readonly List<RippleSegment> segments = new List<RippleSegment>();
    private float elapsed;

    public static void Spawn(Vector2 position)
    {
        GameObject rippleObject = new GameObject("Water Cleaning Ripple");
        rippleObject.transform.position = position;
        rippleObject.AddComponent<WaterCleaningRipple>();
    }

    private void Start()
    {
        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = Mathf.PI * 2f * i / SegmentCount;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.55f);

            GameObject segmentObject = new GameObject("Ripple Pixel");
            segmentObject.transform.SetParent(transform, false);
            segmentObject.transform.localScale = Vector3.one * 0.06f;

            SpriteRenderer renderer = segmentObject.AddComponent<SpriteRenderer>();
            renderer.sprite = WaterImpactSplash.GetPixelSprite();
            renderer.sortingOrder = 1;
            renderer.color = i % 2 == 0
                ? new Color(0.86f, 0.98f, 1f, 1f)
                : new Color(0.42f, 0.84f, 1f, 1f);

            segments.Add(new RippleSegment
            {
                Renderer = renderer,
                Direction = direction.normalized,
                VerticalScale = direction.y == 0f ? 0.55f : Mathf.Abs(direction.y / direction.normalized.y)
            });
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Lifetime);
        float radius = Mathf.Lerp(0.12f, 0.9f, progress);

        foreach (RippleSegment segment in segments)
        {
            if (segment.Renderer == null)
            {
                continue;
            }

            Vector2 offset = segment.Direction * radius;
            offset.y *= segment.VerticalScale;
            segment.Renderer.transform.localPosition = offset;

            Color color = segment.Renderer.color;
            color.a = 1f - progress;
            segment.Renderer.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private class RippleSegment
    {
        public SpriteRenderer Renderer;
        public Vector2 Direction;
        public float VerticalScale;
    }
}
