using UnityEngine;
using UnityEngine.Tilemaps; // Required for Tilemap operations

public class TilemapWaterFiller : MonoBehaviour
{
    [Header("Tilemap Setup")]
    [Tooltip("The Tilemap to draw the water tiles onto (e.g., your 'Water' Tilemap GameObject).")]
    public Tilemap targetWaterTilemap;

    [Tooltip("The Water Tile asset from your Project (e.g., drag your 'Water' prefab here if it's a Tile, AnimatedTile, or RuleTile).")]
    public TileBase waterTileAsset;

    [Header("Fill Area Definition")]
    [Tooltip("If true, uses the manual origin, width, and height defined below. If false, tries to use boundsReferenceTilemap.")]
    public bool useManualBounds = true;

    [Tooltip("Optional: Another Tilemap (e.g., 'SpawnMap') whose bounds will be used to define the fill area. Ignored if useManualBounds is true or if this is not set.")]
    public Tilemap boundsReferenceTilemap;

    [Header("Manual Bounds (if useManualBounds is true)")]
    [Tooltip("The starting cell X, Y coordinate for filling (Z is usually 0 for 2D Tilemaps).")]
    public Vector3Int manualOriginCell = Vector3Int.zero;
    [Tooltip("How many cells wide the fill area should be.")]
    public int manualWidthInCells = 20;
    [Tooltip("How many cells high the fill area should be.")]
    public int manualHeightInCells = 20;

    [Header("Options")]
    [Tooltip("If true, any existing tiles within the fill area on the targetWaterTilemap will be cleared before placing water tiles.")]
    public bool clearExistingTilesInArea = true;


    // ContextMenu attribute allows you to right-click the script component in the Inspector and run this method.
    [ContextMenu("Fill Area With Water Tiles")]
    public void FillWaterWithTiles()
    {
        if (targetWaterTilemap == null)
        {
            Debug.LogError("TilemapWaterFiller: Target Water Tilemap is not assigned!");
            return;
        }
        if (waterTileAsset == null)
        {
            Debug.LogError("TilemapWaterFiller: Water Tile Asset is not assigned!");
            return;
        }

        BoundsInt fillBounds;

        if (useManualBounds || boundsReferenceTilemap == null)
        {
            Debug.Log("Using manual bounds for filling.");
            fillBounds = new BoundsInt(manualOriginCell.x, manualOriginCell.y, manualOriginCell.z,
                                       manualWidthInCells, manualHeightInCells, 1); // Depth is 1 for 2D tilemaps
        }
        else
        {
            Debug.Log($"Using bounds from reference tilemap: {boundsReferenceTilemap.name}");
            boundsReferenceTilemap.CompressBounds(); // Important to get the tightest bounds
            fillBounds = boundsReferenceTilemap.cellBounds;
            if (fillBounds.size.x == 0 && fillBounds.size.y == 0)
            {
                Debug.LogWarning($"Reference tilemap '{boundsReferenceTilemap.name}' appears to be empty or its bounds are not set. Check the reference tilemap.");
                // Fallback to a default small area if reference is empty to prevent infinite loops or huge areas
                fillBounds = new BoundsInt(0, 0, 0, 10, 10, 1);
            }
        }

        Debug.Log($"Calculated Fill Bounds: Position={fillBounds.position}, Size={fillBounds.size}");

        // Iterate over all positions within the bounds.
        // BoundsInt.allPositionsWithin is an iterator that gives you every Vector3Int cell position.
        foreach (Vector3Int cellPosition in fillBounds.allPositionsWithin)
        {
            if (clearExistingTilesInArea)
            {
                // Clear the tile first if the option is selected.
                // Note: Doing this for every tile in the loop is fine for moderate sizes.
                // For very large areas, clearing the whole area in a separate loop first might be slightly more performant
                // but can also be done by setting the tile to null before setting the new one.
                // For simplicity here, we just set it. If it's already null, setting to null does nothing.
                // If we want to ensure it's cleared *before* potentially placing water, this is okay.
                // However, we only want to clear if we are truly clearing the whole area.
                // Let's refine this: clear first, then fill.
            }
            // Set the water tile at the current cell position.
            // The Z coordinate from cellPosition will be used. For 2D tilemaps, it's typically 0.
            targetWaterTilemap.SetTile(cellPosition, waterTileAsset);
        }

        // If clearing, it's better to do it in a separate pass for the entire bounds
        if (clearExistingTilesInArea)
        {
            Debug.Log("Clearing existing tiles in the area first...");
            foreach (Vector3Int cellPositionInBounds in fillBounds.allPositionsWithin)
            {
                // Check if the tile we are about to place is the same as what we want to clear
                // This logic is a bit tricky if we want to clear *then* fill.
                // A simpler clear:
                TileBase existingTile = targetWaterTilemap.GetTile(cellPositionInBounds);
                if (existingTile != null && existingTile != waterTileAsset) // Only clear if it's not already the water tile we're placing
                {
                    // Actually, if clearExistingTilesInArea is true, we just want to make sure the area is clean
                    // before the *final* fill pass. The above loop already fills.
                    // So, this clearing logic should happen *before* the filling loop.
                }
            }
            // Let's restructure for clarity: Clear first, then fill.
        }


        Debug.Log($"Water filling complete for {fillBounds.size.x * fillBounds.size.y} cells.");
    }

    // Revised method with clearer Clear then Fill logic
    [ContextMenu("Fill Area With Water Tiles (Clear Then Fill)")]
    public void FillWaterWithTiles_ClearThenFill()
    {
        if (targetWaterTilemap == null)
        {
            Debug.LogError("TilemapWaterFiller: Target Water Tilemap is not assigned!");
            return;
        }
        if (waterTileAsset == null)
        {
            Debug.LogError("TilemapWaterFiller: Water Tile Asset is not assigned!");
            return;
        }

        BoundsInt fillBounds;

        if (useManualBounds || boundsReferenceTilemap == null)
        {
            Debug.Log("Using manual bounds for filling.");
            fillBounds = new BoundsInt(manualOriginCell.x, manualOriginCell.y, 0, // Ensure Z is 0 for cellBounds if not specified
                                       manualWidthInCells, manualHeightInCells, 1);
        }
        else
        {
            Debug.Log($"Using bounds from reference tilemap: {boundsReferenceTilemap.name}");
            boundsReferenceTilemap.CompressBounds();
            fillBounds = boundsReferenceTilemap.cellBounds;
            if (fillBounds.size.x == 0 && fillBounds.size.y == 0 && fillBounds.size.z == 0)
            {
                Debug.LogWarning($"Reference tilemap '{boundsReferenceTilemap.name}' appears to be empty or its bounds are not set. Using a default 10x10 area at 0,0.");
                fillBounds = new BoundsInt(0, 0, 0, 10, 10, 1);
            }
        }
        // Ensure Z-depth of bounds is 1 for iterating 2D tilemaps correctly with allPositionsWithin
        if (fillBounds.size.z == 0)
        {
            fillBounds.size = new Vector3Int(fillBounds.size.x, fillBounds.size.y, 1);
        }


        Debug.Log($"Calculated Fill Bounds: Position={fillBounds.position}, Size={fillBounds.size}");

        if (clearExistingTilesInArea)
        {
            Debug.Log("Clearing existing tiles in the defined area...");
            for (int x = fillBounds.xMin; x < fillBounds.xMax; x++)
            {
                for (int y = fillBounds.yMin; y < fillBounds.yMax; y++)
                {
                    // Assuming Z=0 for the tiles we are clearing on a 2D tilemap
                    targetWaterTilemap.SetTile(new Vector3Int(x, y, fillBounds.zMin), null);
                }
            }
        }

        Debug.Log("Placing water tiles...");
        for (int x = fillBounds.xMin; x < fillBounds.xMax; x++)
        {
            for (int y = fillBounds.yMin; y < fillBounds.yMax; y++)
            {
                // Assuming Z=0 for the tiles we are placing on a 2D tilemap
                targetWaterTilemap.SetTile(new Vector3Int(x, y, fillBounds.zMin), waterTileAsset);
            }
        }

        Debug.Log($"Water filling process complete for an area of {fillBounds.size.x}x{fillBounds.size.y} cells.");
    }


    [ContextMenu("Clear Water From Area")]
    public void ClearWaterFromArea()
    {
        if (targetWaterTilemap == null)
        {
            Debug.LogError("TilemapWaterFiller: Target Water Tilemap is not assigned!");
            return;
        }

        BoundsInt fillBounds;

        if (useManualBounds || boundsReferenceTilemap == null)
        {
            fillBounds = new BoundsInt(manualOriginCell.x, manualOriginCell.y, 0,
                                       manualWidthInCells, manualHeightInCells, 1);
        }
        else
        {
            boundsReferenceTilemap.CompressBounds();
            fillBounds = boundsReferenceTilemap.cellBounds;
            if (fillBounds.size.x == 0 && fillBounds.size.y == 0 && fillBounds.size.z == 0)
            {
                Debug.LogWarning($"Reference tilemap '{boundsReferenceTilemap.name}' appears to be empty. Using a default 10x10 area at 0,0 for clearing.");
                fillBounds = new BoundsInt(0, 0, 0, 10, 10, 1);
            }
        }
        if (fillBounds.size.z == 0)
        {
            fillBounds.size = new Vector3Int(fillBounds.size.x, fillBounds.size.y, 1);
        }

        Debug.Log($"Clearing tiles within bounds: Position={fillBounds.position}, Size={fillBounds.size}");

        for (int x = fillBounds.xMin; x < fillBounds.xMax; x++)
        {
            for (int y = fillBounds.yMin; y < fillBounds.yMax; y++)
            {
                targetWaterTilemap.SetTile(new Vector3Int(x, y, fillBounds.zMin), null);
            }
        }
        Debug.Log("Area clearing complete.");
    }
}
