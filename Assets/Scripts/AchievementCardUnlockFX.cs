using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds a gentle presentation effect to an unlocked achievement card.
/// Attach this to the card parent and assign its visual image layers.
/// </summary>
public class AchievementCardUnlockFX : MonoBehaviour
{
    [Header("Card Layers")]
    [SerializeField] private RectTransform[] cardLayers;

    [Header("Unlock Motion")]
    [SerializeField] private bool unlocked = true;
    [SerializeField] private float floatHeight = 12f;
    [SerializeField] private float floatSpeed = 1.25f;
    [SerializeField] private float wobbleAngle = 1.6f;
    [SerializeField] private float wobbleSpeed = 1.1f;

    [Header("Gold Sparkles")]
    [SerializeField, Range(4, 32)] private int sparkleCount = 14;
    [SerializeField] private Color sparkleColor = new Color(1f, 0.75f, 0.2f, 1f);
    [SerializeField] private Vector2 sparkleSizeRange = new Vector2(4f, 9f);
    [SerializeField] private float sparkleSpeed = 1.8f;
    [SerializeField] private float sparklePadding = 26f;

    private readonly List<Vector2> basePositions = new();
    private readonly List<float> sparklePhases = new();
    private readonly List<Image> sparkles = new();
    private Vector2 cardCenter;
    private Vector2 cardSize;

    private void Awake()
    {
        foreach (RectTransform layer in cardLayers)
        {
            if (layer != null)
            {
                basePositions.Add(layer.anchoredPosition);
            }
        }
    }

    private void Start()
    {
        if (!unlocked || cardLayers == null || cardLayers.Length == 0 || cardLayers[0] == null)
        {
            enabled = false;
            return;
        }

        cardCenter = cardLayers[0].anchoredPosition;
        cardSize = cardLayers[0].rect.size;
        CreateSparkles();
    }

    private void OnEnable()
    {
        SetSparkleVisibility(true);
    }

    private void OnDisable()
    {
        SetSparkleVisibility(false);
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float verticalOffset = Mathf.Sin(time * floatSpeed) * floatHeight;
        float rotation = Mathf.Sin(time * wobbleSpeed) * wobbleAngle;
        int positionIndex = 0;

        foreach (RectTransform layer in cardLayers)
        {
            if (layer == null)
            {
                continue;
            }

            layer.anchoredPosition = basePositions[positionIndex++] + Vector2.up * verticalOffset;
            layer.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        for (int i = 0; i < sparkles.Count; i++)
        {
            float pulse = Mathf.Clamp01(Mathf.Sin(time * sparkleSpeed + sparklePhases[i]) * 0.7f + 0.35f);
            Color color = sparkleColor;
            color.a = pulse;
            sparkles[i].color = color;
            sparkles[i].rectTransform.localScale = Vector3.one * (0.7f + pulse * 0.55f);
        }
    }

    private void CreateSparkles()
    {
        System.Random random = new System.Random(gameObject.GetInstanceID());

        for (int i = 0; i < sparkleCount; i++)
        {
            GameObject sparkleObject = new GameObject($"Gold Sparkle {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform sparkleTransform = sparkleObject.GetComponent<RectTransform>();
            sparkleTransform.SetParent(transform, false);
            sparkleTransform.anchorMin = new Vector2(0.5f, 0.5f);
            sparkleTransform.anchorMax = new Vector2(0.5f, 0.5f);
            sparkleTransform.pivot = new Vector2(0.5f, 0.5f);

            float angle = (Mathf.PI * 2f / sparkleCount) * i + RandomRange(random, -0.2f, 0.2f);
            float radiusX = cardSize.x * 0.5f + sparklePadding + RandomRange(random, 0f, 22f);
            float radiusY = cardSize.y * 0.5f + sparklePadding + RandomRange(random, 0f, 22f);
            sparkleTransform.anchoredPosition = cardCenter + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            sparkleTransform.sizeDelta = Vector2.one * RandomRange(random, sparkleSizeRange.x, sparkleSizeRange.y);

            Image sparkle = sparkleObject.GetComponent<Image>();
            sparkle.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            sparkle.raycastTarget = false;
            sparkle.color = sparkleColor;
            sparkles.Add(sparkle);
            sparklePhases.Add(RandomRange(random, 0f, Mathf.PI * 2f));
        }
    }

    private void SetSparkleVisibility(bool visible)
    {
        foreach (Image sparkle in sparkles)
        {
            if (sparkle != null)
            {
                sparkle.gameObject.SetActive(visible);
            }
        }
    }

    private static float RandomRange(System.Random random, float minimum, float maximum)
    {
        return minimum + (float)random.NextDouble() * (maximum - minimum);
    }
}
