using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the water-cleanup loop active by recreating cleaned trash only after
/// the boat has travelled far enough away from that trash's original location.
/// </summary>
public sealed class WaterTrashRespawnManager : MonoBehaviour
{
    private sealed class RespawnRequest
    {
        public GameObject template;
        public Vector3 position;
        public Quaternion rotation;
    }

    private static WaterTrashRespawnManager instance;
    private static bool isCreatingTemplate;

    [SerializeField, Min(0.1f)] private float respawnDistance = 24f;

    private readonly List<RespawnRequest> pendingRespawns = new();
    private BoatController boat;

    /// <summary>Used to prevent an inactive template from creating pollution effects.</summary>
    public static bool IsCreatingTemplate => isCreatingTemplate;

    public static WaterTrashRespawnManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject managerObject = new("Water Trash Respawn Manager");
        instance = managerObject.AddComponent<WaterTrashRespawnManager>();
        return instance;
    }

    /// <summary>Queues an identical water-trash item for the original spawn point.</summary>
    public void QueueRespawn(GameObject source, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (source == null)
        {
            return;
        }

        isCreatingTemplate = true;
        GameObject template = Instantiate(source, transform);
        isCreatingTemplate = false;

        template.name = source.name;
        template.SetActive(false);

        pendingRespawns.Add(new RespawnRequest
        {
            template = template,
            position = spawnPosition,
            rotation = spawnRotation
        });
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (pendingRespawns.Count == 0)
        {
            return;
        }

        if (boat == null)
        {
            boat = FindFirstObjectByType<BoatController>();
        }

        if (boat == null)
        {
            return;
        }

        float minimumDistanceSqr = respawnDistance * respawnDistance;
        for (int index = pendingRespawns.Count - 1; index >= 0; index--)
        {
            RespawnRequest request = pendingRespawns[index];
            if (request.template == null)
            {
                pendingRespawns.RemoveAt(index);
                continue;
            }

            if (((Vector2)(boat.transform.position - request.position)).sqrMagnitude < minimumDistanceSqr)
            {
                continue;
            }

            GameObject replacement = Instantiate(request.template, request.position, request.rotation);
            replacement.name = request.template.name;
            replacement.SetActive(true);

            Destroy(request.template);
            pendingRespawns.RemoveAt(index);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
