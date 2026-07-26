using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Applies the toxic-water treatment to BadEnding's front-most water detail tilemap.
/// The component is attached to the order-2 Extra Detail grid, not the base water grid.
/// </summary>
[DisallowMultipleComponent]
public sealed class BadEndingToxicOcean : MonoBehaviour
{
    [Header("Biohazard Palette")]
    [SerializeField] private Color oilColor = new(0.025f, 0.10f, 0.12f, 1f);
    [SerializeField] private Color muckColor = new(0.13f, 0.26f, 0.24f, 1f);
    [SerializeField] private Color toxicColor = new(0.48f, 0.76f, 0.12f, 1f);
    [SerializeField] private Color hazardColor = new(0.80f, 0.96f, 0.22f, 1f);

    [Header("Contamination Motion")]
    [SerializeField, Range(0.1f, 4f)] private float flowSpeed = 0.26f;
    [SerializeField, Range(0.1f, 8f)] private float contaminationScale = 1.45f;
    [SerializeField, Range(0f, 1f)] private float toxicStrength = 0.58f;
    [SerializeField, Range(0f, 1f)] private float glowStrength = 0.14f;

    private const string ToxicOceanShader = "MicroBit/Toxic Ocean";

    private TilemapRenderer targetRenderer;
    private Material originalMaterial;
    private Material toxicMaterial;

    private void Awake()
    {
        ApplyToxicMaterial();
    }

    private void OnEnable()
    {
        ApplyToxicMaterial();
    }

    private void OnDisable()
    {
        RestoreOriginalMaterial();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterial();

        if (toxicMaterial != null)
        {
            Destroy(toxicMaterial);
        }
    }

    private void ApplyToxicMaterial()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<TilemapRenderer>(true);
            if (targetRenderer == null)
            {
                Debug.LogWarning($"{nameof(BadEndingToxicOcean)} needs a TilemapRenderer below Extra Detail.", this);
                return;
            }

            originalMaterial = targetRenderer.sharedMaterial;
        }

        if (toxicMaterial == null)
        {
            Shader shader = Shader.Find(ToxicOceanShader);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(BadEndingToxicOcean)} could not find {ToxicOceanShader}.", this);
                return;
            }

            toxicMaterial = new Material(shader)
            {
                name = "Bad Ending Toxic Ocean (Runtime)"
            };
            ConfigureMaterial();
        }

        targetRenderer.sharedMaterial = toxicMaterial;
    }

    private void ConfigureMaterial()
    {
        toxicMaterial.SetColor("_OilColor", oilColor);
        toxicMaterial.SetColor("_MuckColor", muckColor);
        toxicMaterial.SetColor("_ToxicColor", toxicColor);
        toxicMaterial.SetColor("_HazardColor", hazardColor);
        toxicMaterial.SetFloat("_FlowSpeed", flowSpeed);
        toxicMaterial.SetFloat("_ContaminationScale", contaminationScale);
        toxicMaterial.SetFloat("_ToxicStrength", toxicStrength);
        toxicMaterial.SetFloat("_GlowStrength", glowStrength);
    }

    private void RestoreOriginalMaterial()
    {
        if (targetRenderer != null)
        {
            targetRenderer.sharedMaterial = originalMaterial;
        }
    }
}
