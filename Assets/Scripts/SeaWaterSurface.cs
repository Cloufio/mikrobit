using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Gives a water tilemap either the original soft-water look or a coast-aware surface.
/// The original material is restored whenever this component is disabled or destroyed.
/// </summary>
[DisallowMultipleComponent]
public class SeaWaterSurface : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private string waterTilemapName = "OuterMap";
    [SerializeField] private bool useCoastalGradient;

    [Header("Coastal Depth")]
    [SerializeField] private Tilemap coastTilemap;
    [SerializeField] private string coastTilemapName = "RandomizedMap (7)";
    [SerializeField, Min(1f)] private float shoreGradientDistance = 28f;
    [SerializeField] private Color shoreColor = new Color(0.72f, 0.72f, 0.57f, 1f);
    [SerializeField] private Color oceanColor = new Color(0.30f, 0.57f, 0.70f, 1f);

    [Header("Palette")]
    [SerializeField] private Color deepColor = new Color(0.24f, 0.48f, 0.63f, 1f);
    [SerializeField] private Color shallowColor = new Color(0.56f, 0.72f, 0.76f, 1f);
    [SerializeField] private Color rippleColor = new Color(0.90f, 0.98f, 0.96f, 1f);
    [SerializeField, Range(0.02f, 0.5f)] private float waterHueThreshold = 0.14f;

    [Header("Surface Motion")]
    [SerializeField, Range(0.1f, 4f)] private float flowSpeed = 0.55f;
    [SerializeField, Range(0.1f, 8f)] private float waveScale = 2.2f;
    [SerializeField, Range(0f, 1f)] private float rippleStrength = 0.5f;
    [SerializeField, Range(0.1f, 4f)] private float microRippleScale = 1.65f;
    [SerializeField, Range(0f, 1f)] private float microRippleStrength = 0.58f;
    [SerializeField, Range(0.01f, 0.25f)] private float microRippleLineWidth = 0.09f;
    [SerializeField, Range(0.05f, 3f)] private float microRippleSpeed = 0.65f;
    [SerializeField, Range(0f, 0.75f)] private float microRippleMotion = 0.18f;
    [SerializeField, Range(0.1f, 2f)] private float glintDensity = 0.65f;
    [SerializeField, Range(0.1f, 5f)] private float glintBlinkSpeed = 0.8f;
    [SerializeField, Range(0f, 1f)] private float sparkleStrength = 0.42f;
    [SerializeField, Range(1f, 64f)] private float pixelDensity = 16f;

    private const string SoftWaterShaderName = "MicroBit/Soft Animated Water";
    private const string CoastalWaterShaderName = "MicroBit/Coastal Animated Water";

    private TilemapRenderer waterRenderer;
    private Material originalMaterial;
    private Material runtimeMaterial;
    private Texture2D shoreDistanceTexture;

    private void Awake()
    {
        ApplyWaterMaterial();
    }

    private void OnEnable()
    {
        ApplyWaterMaterial();
    }

    private void Update()
    {
        UpdateMaterialProperties();
    }

    private void OnDisable()
    {
        RestoreOriginalMaterial();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterial();

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }

        if (shoreDistanceTexture != null)
        {
            Destroy(shoreDistanceTexture);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && useCoastalGradient)
        {
            BuildShoreDistanceMap();
        }

        UpdateMaterialProperties();
    }

    private void ApplyWaterMaterial()
    {
        if (waterTilemap == null)
        {
            GameObject waterObject = GameObject.Find(waterTilemapName);
            if (waterObject != null)
            {
                waterTilemap = waterObject.GetComponent<Tilemap>();
            }
        }

        if (waterTilemap == null)
        {
            return;
        }

        waterRenderer = waterTilemap.GetComponent<TilemapRenderer>();
        string shaderName = useCoastalGradient ? CoastalWaterShaderName : SoftWaterShaderName;
        Shader waterShader = Shader.Find(shaderName);
        if (waterRenderer == null || waterShader == null)
        {
            Debug.LogWarning("Water surface could not find its target renderer or shader.", this);
            return;
        }

        if (runtimeMaterial != null && runtimeMaterial.shader != waterShader)
        {
            RestoreOriginalMaterial();
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }

        if (runtimeMaterial == null)
        {
            originalMaterial = waterRenderer.sharedMaterial;
            runtimeMaterial = new Material(waterShader)
            {
                name = useCoastalGradient ? "Coastal Animated Water (Runtime)" : "Soft Animated Water (Runtime)"
            };
        }

        waterRenderer.sharedMaterial = runtimeMaterial;

        if (useCoastalGradient)
        {
            BuildShoreDistanceMap();
        }

        UpdateMaterialProperties();
    }

    private void UpdateMaterialProperties()
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        runtimeMaterial.SetColor("_DeepColor", deepColor);
        runtimeMaterial.SetColor("_ShallowColor", shallowColor);
        runtimeMaterial.SetColor("_RippleColor", rippleColor);
        runtimeMaterial.SetFloat("_WaterHueThreshold", waterHueThreshold);
        runtimeMaterial.SetFloat("_FlowSpeed", flowSpeed);
        runtimeMaterial.SetFloat("_WaveScale", waveScale);
        runtimeMaterial.SetFloat("_RippleStrength", rippleStrength);
        runtimeMaterial.SetFloat("_MicroRippleScale", microRippleScale);
        runtimeMaterial.SetFloat("_MicroRippleStrength", microRippleStrength);
        runtimeMaterial.SetFloat("_MicroRippleLineWidth", microRippleLineWidth);
        runtimeMaterial.SetFloat("_MicroRippleSpeed", microRippleSpeed);
        runtimeMaterial.SetFloat("_MicroRippleMotion", microRippleMotion);
        runtimeMaterial.SetFloat("_GlintDensity", glintDensity);
        runtimeMaterial.SetFloat("_GlintBlinkSpeed", glintBlinkSpeed);
        runtimeMaterial.SetFloat("_SparkleStrength", sparkleStrength);
        runtimeMaterial.SetFloat("_PixelDensity", pixelDensity);

        if (!useCoastalGradient)
        {
            return;
        }

        runtimeMaterial.SetColor("_ShoreColor", shoreColor);
        runtimeMaterial.SetColor("_OceanColor", oceanColor);

        if (shoreDistanceTexture != null)
        {
            runtimeMaterial.SetTexture("_ShoreDistanceTex", shoreDistanceTexture);
        }
    }

    [ContextMenu("Rebuild Shore Gradient")]
    private void BuildShoreDistanceMap()
    {
        if (!useCoastalGradient || waterTilemap == null)
        {
            return;
        }

        if (coastTilemap == null)
        {
            GameObject coastObject = GameObject.Find(coastTilemapName);
            if (coastObject != null)
            {
                coastTilemap = coastObject.GetComponent<Tilemap>();
            }
        }

        if (coastTilemap == null)
        {
            Debug.LogWarning("Coastal water needs a coast tilemap to calculate depth.", this);
            return;
        }

        BoundsInt waterBounds = waterTilemap.cellBounds;
        if (waterBounds.size.x <= 0 || waterBounds.size.y <= 0)
        {
            return;
        }

        var coastPositions = new System.Collections.Generic.List<Vector2>();
        BoundsInt coastBounds = coastTilemap.cellBounds;

        for (int y = coastBounds.yMin; y < coastBounds.yMax; y++)
        {
            for (int x = coastBounds.xMin; x < coastBounds.xMax; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (coastTilemap.HasTile(cell))
                {
                    coastPositions.Add(coastTilemap.GetCellCenterWorld(cell));
                }
            }
        }

        if (coastPositions.Count == 0)
        {
            Debug.LogWarning("Coastal water did not find any painted coast tiles.", this);
            return;
        }

        int width = waterBounds.size.x;
        int height = waterBounds.size.y;

        if (shoreDistanceTexture == null || shoreDistanceTexture.width != width || shoreDistanceTexture.height != height)
        {
            if (shoreDistanceTexture != null)
            {
                Destroy(shoreDistanceTexture);
            }

            shoreDistanceTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "Shore Distance Map (Runtime)",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        var pixels = new Color[width * height];
        float inverseGradientDistance = 1f / shoreGradientDistance;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int waterCell = new Vector3Int(waterBounds.xMin + x, waterBounds.yMin + y, 0);
                Vector2 waterPosition = waterTilemap.GetCellCenterWorld(waterCell);
                float nearestDistanceSquared = float.PositiveInfinity;

                for (int i = 0; i < coastPositions.Count; i++)
                {
                    float distanceSquared = (coastPositions[i] - waterPosition).sqrMagnitude;
                    if (distanceSquared < nearestDistanceSquared)
                    {
                        nearestDistanceSquared = distanceSquared;
                    }
                }

                float normalizedDistance = Mathf.Clamp01(Mathf.Sqrt(nearestDistanceSquared) * inverseGradientDistance);
                pixels[y * width + x] = new Color(normalizedDistance, 0f, 0f, 1f);
            }
        }

        shoreDistanceTexture.SetPixels(pixels);
        shoreDistanceTexture.Apply(false, false);

        Vector3 worldMin = waterTilemap.CellToWorld(waterBounds.min);
        Vector3 worldMax = waterTilemap.CellToWorld(waterBounds.max);
        Vector3 worldSize = worldMax - worldMin;

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetTexture("_ShoreDistanceTex", shoreDistanceTexture);
            runtimeMaterial.SetVector("_ShoreMapWorldMin", worldMin);
            runtimeMaterial.SetVector("_ShoreMapWorldSize", new Vector4(Mathf.Max(0.001f, worldSize.x), Mathf.Max(0.001f, worldSize.y), 0f, 0f));
        }
    }

    private void RestoreOriginalMaterial()
    {
        if (waterRenderer != null && originalMaterial != null && waterRenderer.sharedMaterial == runtimeMaterial)
        {
            waterRenderer.sharedMaterial = originalMaterial;
        }
    }
}
