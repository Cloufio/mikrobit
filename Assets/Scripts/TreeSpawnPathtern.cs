using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGeneration : MonoBehaviour {
    public GameObject[] objects;

    void Start()
    {
        if (ComboSpawnPool.TryGetSpawnPrefab(out GameObject comboPrefab))
        {
            Instantiate(comboPrefab, transform.position, Quaternion.identity);
            return;
        }

        if (objects == null || objects.Length == 0)
        {
            return;
        }

        int rand = Random.Range(0, objects.Length);
        if (objects[rand] != null)
        {
            Instantiate(objects[rand], transform.position, Quaternion.identity);
        }
    }
}
