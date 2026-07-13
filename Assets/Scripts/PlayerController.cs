using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Animator animator;
    Vector2 movement;

    public Vector2 lastPos;

    private Tilemap[] waterTilemaps;

    void Update()
    {
        movement.x = Input.GetAxis("Horizontal");
        movement.y = Input.GetAxis("Vertical");

        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetFloat("Speed", movement.sqrMagnitude);

        //if (movement.x != 0) movement.y = 0;

        if (movement.x != 0 || movement.y != 0)
        {
            lastPos = new Vector2(movement.x, movement.y).normalized;
        }
    }

    void FixedUpdate()
    {
        Vector2 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;
        if (!IsPositionWater(targetPosition))
        {
            rb.MovePosition(targetPosition);
        }
        else
        {
            // Allow sliding along the shore by checking X and Y movements separately
            Vector2 targetX = rb.position + new Vector2(movement.x, 0) * moveSpeed * Time.fixedDeltaTime;
            Vector2 targetY = rb.position + new Vector2(0, movement.y) * moveSpeed * Time.fixedDeltaTime;

            bool xWater = IsPositionWater(targetX);
            bool yWater = IsPositionWater(targetY);

            if (!xWater && movement.x != 0)
            {
                rb.MovePosition(targetX);
            }
            else if (!yWater && movement.y != 0)
            {
                rb.MovePosition(targetY);
            }
            else
            {
                // Fully blocked, stop velocity to prevent jittering
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    void FindWaterTilemaps()
    {
        var waterList = new List<Tilemap>();

        // 1. Search by TilemapWaterFiller script
        var filler = FindObjectOfType<TilemapWaterFiller>();
        if (filler != null && filler.targetWaterTilemap != null)
        {
            waterList.Add(filler.targetWaterTilemap);
        }

        // 2. Search by GameObject name containing "water"
        var allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (var tm in allTilemaps)
        {
            if (tm.gameObject.name.ToLower().Contains("water"))
            {
                if (!waterList.Contains(tm))
                {
                    waterList.Add(tm);
                }
            }
        }

        waterTilemaps = waterList.ToArray();
    }

    public bool IsPositionWater(Vector3 worldPosition)
    {
        if (waterTilemaps == null || waterTilemaps.Length == 0)
        {
            FindWaterTilemaps();
        }

        // Check our cached water tilemaps first
        foreach (var tm in waterTilemaps)
        {
            Vector3Int cell = tm.WorldToCell(worldPosition);
            if (tm.HasTile(cell))
            {
                if (HasWalkableTileAt(worldPosition))
                {
                    return false;
                }
                return true;
            }
        }

        // Fallback: check all tilemaps to see if any tile at this position contains "water" in its name
        var allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (var tm in allTilemaps)
        {
            Vector3Int cell = tm.WorldToCell(worldPosition);
            if (tm.HasTile(cell))
            {
                var tile = tm.GetTile(cell);
                if (tile != null && tile.name.ToLower().Contains("water"))
                {
                    if (HasWalkableTileAt(worldPosition))
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasWalkableTileAt(Vector3 worldPosition)
    {
        var allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (var tm in allTilemaps)
        {
            string mapName = tm.gameObject.name.ToLower();
            if (mapName.Contains("water"))
            {
                continue; // Skip water tilemaps
            }

            Vector3Int cell = tm.WorldToCell(worldPosition);
            if (tm.HasTile(cell))
            {
                var tile = tm.GetTile(cell);
                if (tile != null)
                {
                    string tileName = tile.name.ToLower();
                    if (tileName.Contains("bridge") || tileName.Contains("floor") || tileName.Contains("ground") || tileName.Contains("land") || tileName.Contains("grass") ||
                        mapName.Contains("bridge") || mapName.Contains("floor") || mapName.Contains("ground") || mapName.Contains("land") || mapName.Contains("grass"))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
