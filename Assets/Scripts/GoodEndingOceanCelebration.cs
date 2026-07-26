using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Applies a bright, animated clean-ocean material to every water tilemap below this object.
/// It is deliberately attached only to GoodEnding's Water grid.
/// </summary>
[DisallowMultipleComponent]
public sealed class GoodEndingOceanCelebration : MonoBehaviour
{
    [Header("Clean Ocean Palette")]
    [SerializeField] private Color deepColor = new(0.10f, 0.48f, 0.63f, 1f);
    [SerializeField] private Color shallowColor = new(0.35f, 0.78f, 0.82f, 1f);
    [SerializeField] private Color foamColor = new(0.92f, 1f, 0.88f, 1f);

    [Header("Gentle Motion")]
    [SerializeField, Range(0.1f, 4f)] private float flowSpeed = 0.38f;
    [SerializeField, Range(0.1f, 8f)] private float waveScale = 1.25f;
    [SerializeField, Range(0f, 1f)] private float rippleStrength = 0.42f;
    [SerializeField, Range(0f, 1f)] private float glintStrength = 0.24f;

    private const string CelebrationOceanShader = "MicroBit/Celebration Ocean";

    private readonly List<TilemapRenderer> waterRenderers = new();
    private readonly List<Material> originalMaterials = new();
    private Material celebrationMaterial;

    private void Awake()
    {
        ApplyCelebrationMaterial();
    }

    private void OnEnable()
    {
        ApplyCelebrationMaterial();
    }

    private void OnDisable()
    {
        RestoreOriginalMaterials();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterials();

        if (celebrationMaterial != null)
        {
            Destroy(celebrationMaterial);
        }
    }

    private void ApplyCelebrationMaterial()
    {
        if (celebrationMaterial == null)
        {
            Shader shader = Shader.Find(CelebrationOceanShader);
            if (shader == null)
            {
                Debug.LogWarning($"{nameof(GoodEndingOceanCelebration)} could not find {CelebrationOceanShader}.", this);
                return;
            }

            celebrationMaterial = new Material(shader)
            {
                name = "Good Ending Clean Ocean (Runtime)"
            };
            ConfigureMaterial();
        }

        if (waterRenderers.Count == 0)
        {
            foreach (TilemapRenderer renderer in GetComponentsInChildren<TilemapRenderer>(true))
            {
                waterRenderers.Add(renderer);
                originalMaterials.Add(renderer.sharedMaterial);
            }
        }

        foreach (TilemapRenderer renderer in waterRenderers)
        {
            if (renderer != null)
            {
                renderer.sharedMaterial = celebrationMaterial;
            }
        }
    }

    private void ConfigureMaterial()
    {
        celebrationMaterial.SetColor("_DeepColor", deepColor);
        celebrationMaterial.SetColor("_ShallowColor", shallowColor);
        celebrationMaterial.SetColor("_FoamColor", foamColor);
        celebrationMaterial.SetFloat("_FlowSpeed", flowSpeed);
        celebrationMaterial.SetFloat("_WaveScale", waveScale);
        celebrationMaterial.SetFloat("_RippleStrength", rippleStrength);
        celebrationMaterial.SetFloat("_GlintStrength", glintStrength);
    }

    private void RestoreOriginalMaterials()
    {
        for (int index = 0; index < waterRenderers.Count; index++)
        {
            TilemapRenderer renderer = waterRenderers[index];
            if (renderer != null)
            {
                renderer.sharedMaterial = originalMaterials[index];
            }
        }
    }
}
