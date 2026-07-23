using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Gives AchievementScene TMP labels the same gold, outline, and shadow treatment as the gameplay HUD.
/// </summary>
public class AchievementTextStyle : MonoBehaviour
{
    [Header("HUD Palette")]
    [SerializeField] private Color faceColor = new Color(1f, 0.8784314f, 0.6039216f, 1f);
    [SerializeField] private Color outlineColor = new Color(0.3529412f, 0.1882353f, 0.2f, 1f);
    [SerializeField] private Color shadowColor = new Color(0.7607843f, 0.36078432f, 0.23137255f, 1f);

    [Header("Pixel Effects")]
    [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.14f;
    [SerializeField] private Vector2 shadowOffset = new Vector2(0.25f, -0.25f);
    [SerializeField, Range(-1f, 1f)] private float shadowDilate = 0.08f;
    [SerializeField, Range(0f, 1f)] private float shadowSoftness = 0f;

    private readonly List<Material> runtimeMaterials = new();

    private void Awake()
    {
        ApplyStyle();
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            Destroy(material);
        }
    }

    public void ApplyStyle()
    {
        foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.fontSharedMaterial == null)
            {
                continue;
            }

            Material material = new Material(label.fontSharedMaterial);
            runtimeMaterials.Add(material);
            label.fontMaterial = material;
            label.color = faceColor;

            SetColor(material, "_FaceColor", faceColor);
            SetColor(material, "_OutlineColor", outlineColor);
            SetFloat(material, "_OutlineWidth", outlineWidth);
            SetColor(material, "_UnderlayColor", shadowColor);
            SetFloat(material, "_UnderlayOffsetX", shadowOffset.x);
            SetFloat(material, "_UnderlayOffsetY", shadowOffset.y);
            SetFloat(material, "_UnderlayDilate", shadowDilate);
            SetFloat(material, "_UnderlaySoftness", shadowSoftness);
        }
    }

    private static void SetColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }
}
