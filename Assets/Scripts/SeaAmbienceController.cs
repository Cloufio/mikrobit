using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds a calm, non-interactable sea-decoration layer from the existing WaterDetail tiles.
/// </summary>
public class SeaAmbienceController : MonoBehaviour
{
    private const int SurfacePatchCount = 44;
    private const int ShorelineFoamCount = 14;
    private const int GlintClusterCount = 10;
    private const float SpriteRefreshInterval = 0.18f;
    private const int PassiveWaterSortingOrder = 1;

    // Only these animated tiles are allowed to feed the ambient ripple layer.
    // Other water assets remain visible on their tilemaps but receive no extra effect.
    private static readonly HashSet<string> RippleTileNames = new HashSet<string>
    {
        "WaterDetail1",
        "WaterDetail2",
        "WaterDetail3",
        "WaterDetail4",
        "WaterDetail5",
        "WaterDetail6",
        "WaterDetail7",
        "WaterDetail8"
    };

    private readonly List<Vector3Int> waterCells = new List<Vector3Int>();
    private readonly List<Vector3Int> shorelineCells = new List<Vector3Int>();
    private readonly List<AmbientPatch> ambientPatches = new List<AmbientPatch>();
    private readonly System.Random random = new System.Random(18391);

    private Tilemap waterDetailTilemap;
    private Tilemap[] terrainTilemaps;
    private float nextSpriteRefresh;

    private IEnumerator Start()
    {
        // Give Tilemap animations one frame to initialise before asking them for their current sprites.
        yield return null;

        GameObject waterDetailObject = GameObject.Find("WaterDetail");
        if (waterDetailObject == null)
        {
            yield break;
        }

        waterDetailTilemap = waterDetailObject.GetComponent<Tilemap>();
        if (waterDetailTilemap == null)
        {
            yield break;
        }

        CollectWaterCells();
        if (waterCells.Count == 0)
        {
            yield break;
        }

        terrainTilemaps = FindTerrainTilemaps();
        CollectShorelineCells();
        CreateSurfacePatches();
        CreateShorelineFoam();
        CreateGlintClusters();
    }

    private void Update()
    {
        if (ambientPatches.Count == 0)
        {
            return;
        }

        bool refreshSprite = Time.time >= nextSpriteRefresh;
        if (refreshSprite)
        {
            nextSpriteRefresh = Time.time + SpriteRefreshInterval;
        }

        foreach (AmbientPatch patch in ambientPatches)
        {
            if (patch.Renderer == null)
            {
                continue;
            }

            float drift = Mathf.Sin(Time.time * patch.DriftSpeed + patch.Phase);
            float bob = Mathf.Cos(Time.time * patch.DriftSpeed * 0.6f + patch.Phase);
            patch.Renderer.transform.position = patch.BasePosition + new Vector3(drift * patch.HorizontalDrift, bob * patch.VerticalDrift, 0f);

            Color color = patch.BaseColor;
            color.a *= 0.8f + 0.2f * Mathf.Sin(Time.time * 0.8f + patch.Phase);
            patch.Renderer.color = color;

            if (refreshSprite && patch.SourceCell.HasValue)
            {
                Sprite sprite = waterDetailTilemap.GetSprite(patch.SourceCell.Value);
                if (sprite != null)
                {
                    patch.Renderer.sprite = sprite;
                }
            }
        }
    }

    private void CollectWaterCells()
    {
        BoundsInt bounds = waterDetailTilemap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = waterDetailTilemap.GetTile(cell);
            if (tile != null && RippleTileNames.Contains(tile.name))
            {
                waterCells.Add(cell);
            }
        }
    }

    private Tilemap[] FindTerrainTilemaps()
    {
        Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        List<Tilemap> result = new List<Tilemap>();

        foreach (Tilemap tilemap in allTilemaps)
        {
            if (tilemap != null && tilemap != waterDetailTilemap)
            {
                result.Add(tilemap);
            }
        }

        return result.ToArray();
    }

    private void CollectShorelineCells()
    {
        foreach (Vector3Int cell in waterCells)
        {
            Vector3 center = waterDetailTilemap.GetCellCenterWorld(cell);
            if (IsNearTerrain(center))
            {
                shorelineCells.Add(cell);
            }
        }
    }

    private bool IsNearTerrain(Vector3 center)
    {
        const float checkDistance = 0.72f;
        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        foreach (Tilemap terrain in terrainTilemaps)
        {
            foreach (Vector2 direction in directions)
            {
                Vector3Int neighbour = terrain.WorldToCell(center + (Vector3)(direction * checkDistance));
                if (terrain.HasTile(neighbour))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CreateSurfacePatches()
    {
        foreach (Vector3Int cell in GetDistinctCells(waterCells, SurfacePatchCount))
        {
            CreateTilePatch(
                "Sea Surface Detail",
                cell,
                new Color(0.9f, 0.98f, 1f, 0.34f),
                random.Next(0, 2) == 0 ? 0.82f : 1.08f,
                PassiveWaterSortingOrder,
                0.1f,
                0.035f,
                0.25f + (float)random.NextDouble() * 0.2f);
        }
    }

    private void CreateShorelineFoam()
    {
        List<Vector3Int> source = shorelineCells.Count > 0 ? shorelineCells : waterCells;
        foreach (Vector3Int cell in GetDistinctCells(source, ShorelineFoamCount))
        {
            CreateTilePatch(
                "Shoreline Foam",
                cell,
                new Color(0.82f, 0.96f, 1f, 0.68f),
                0.92f,
                PassiveWaterSortingOrder,
                0.035f,
                0.02f,
                0.16f + (float)random.NextDouble() * 0.1f);
        }
    }

    private void CreateGlintClusters()
    {
        for (int i = 0; i < GlintClusterCount; i++)
        {
            Vector3 center = waterDetailTilemap.GetCellCenterWorld(GetRandomCell(waterCells));
            int glintCount = 2 + random.Next(2);

            for (int glint = 0; glint < glintCount; glint++)
            {
                Vector2 offset = new Vector2(
                    -0.32f + (float)random.NextDouble() * 0.64f,
                    -0.18f + (float)random.NextDouble() * 0.36f);
                CreateGlint(center + (Vector3)offset);
            }
        }
    }

    private void CreateTilePatch(
        string objectName,
        Vector3Int cell,
        Color color,
        float scale,
        int sortingOrder,
        float horizontalDrift,
        float verticalDrift,
        float driftSpeed)
    {
        Sprite sprite = waterDetailTilemap.GetSprite(cell);
        if (sprite == null)
        {
            return;
        }

        GameObject patchObject = new GameObject(objectName);
        patchObject.transform.SetParent(transform, true);
        patchObject.transform.position = waterDetailTilemap.GetCellCenterWorld(cell);
        patchObject.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = patchObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        ambientPatches.Add(new AmbientPatch
        {
            Renderer = renderer,
            SourceCell = cell,
            BasePosition = patchObject.transform.position,
            BaseColor = color,
            HorizontalDrift = horizontalDrift,
            VerticalDrift = verticalDrift,
            DriftSpeed = driftSpeed,
            Phase = (float)random.NextDouble() * Mathf.PI * 2f
        });
    }

    private void CreateGlint(Vector3 position)
    {
        GameObject glintObject = new GameObject("Water Glint");
        glintObject.transform.SetParent(transform, true);
        glintObject.transform.position = position;
        glintObject.transform.localScale = Vector3.one * (0.025f + (float)random.NextDouble() * 0.028f);

        SpriteRenderer renderer = glintObject.AddComponent<SpriteRenderer>();
        renderer.sprite = WaterImpactSplash.GetPixelSprite();
        renderer.color = new Color(0.74f, 0.96f, 1f, 0.38f);
        renderer.sortingOrder = PassiveWaterSortingOrder;

        ambientPatches.Add(new AmbientPatch
        {
            Renderer = renderer,
            BasePosition = position,
            BaseColor = renderer.color,
            HorizontalDrift = 0.035f,
            VerticalDrift = 0.02f,
            DriftSpeed = 0.45f + (float)random.NextDouble() * 0.25f,
            Phase = (float)random.NextDouble() * Mathf.PI * 2f
        });
    }

    private Vector3Int GetRandomCell(List<Vector3Int> cells)
    {
        return cells[random.Next(cells.Count)];
    }

    private List<Vector3Int> GetDistinctCells(List<Vector3Int> source, int count)
    {
        List<Vector3Int> candidates = new List<Vector3Int>(source);
        int selectionCount = Mathf.Min(count, candidates.Count);
        List<Vector3Int> selection = new List<Vector3Int>(selectionCount);

        for (int i = 0; i < selectionCount; i++)
        {
            int index = random.Next(candidates.Count);
            selection.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return selection;
    }

    private class AmbientPatch
    {
        public SpriteRenderer Renderer;
        public Vector3Int? SourceCell;
        public Vector3 BasePosition;
        public Color BaseColor;
        public float HorizontalDrift;
        public float VerticalDrift;
        public float DriftSpeed;
        public float Phase;
    }
}
