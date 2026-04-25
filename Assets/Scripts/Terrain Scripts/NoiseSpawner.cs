using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnItem
{
    public GameObject prefab;
    public float rarity = 10f;
}

public class NoiseSpawner : MonoBehaviour
{
    public Terrain terrain;
    public SpawnItem[] prefabs;
    public SpawnItem[] prefabsAround;
    public int density = 5000;
    public float scale = 20f;
    public float threshold = 0.4f;
    public float minScale = 6.0f;
    public float maxScale = 10.0f;
    public float minScale2 = 4.0f;
    public float maxScale2 = 8.0f;
    public float minDistance = 15.0f;

    [Header("Dopasowanie do pod³o¿a")]
    [Range(0f, 1f)]
    [Tooltip("0 = prosto w górê, 1 = idealnie przylega do pochy³oœci terenu")]
    public float terrainAlignment = 0.5f;

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        Spawn();
    }

    GameObject GetRandomPrefab(SpawnItem[] items)
    {
        float totalWeight = 0f;
        foreach (SpawnItem item in items)
        {
            totalWeight += item.rarity;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (SpawnItem item in items)
        {
            currentWeight += item.rarity;
            if (randomValue <= currentWeight)
            {
                return item.prefab;
            }
        }

        return items[0].prefab;
    }

    void Spawn()
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainSize = data.size;
        Vector3 terrainPosition = terrain.transform.position;

        if (prefabs == null || prefabs.Length == 0)
        {
            return;
        }

        for (int i = 0; i < density; i++)
        {
            float x = Random.Range(0, terrainSize.x);
            float z = Random.Range(0, terrainSize.z);

            float worldX = terrainPosition.x + x;
            float worldZ = terrainPosition.z + z;

            float noise = Mathf.PerlinNoise(worldX / terrainSize.x * scale, worldZ / terrainSize.z * scale);

            if (noise > threshold)
            {
                float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
                Vector3 spawnPos = new Vector3(worldX, y + Vector3.down.y, worldZ);

                bool tooClose = false;
                foreach (Vector3 pos in spawnedPositions)
                {
                    if (Vector3.Distance(spawnPos, pos) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    float normX = x / terrainSize.x;
                    float normZ = z / terrainSize.z;
                    Vector3 terrainNormal = data.GetInterpolatedNormal(normX, normZ);
                    Vector3 blendedNormal = Vector3.Slerp(Vector3.up, terrainNormal, terrainAlignment).normalized;

                    GameObject selectedPrefab = GetRandomPrefab(prefabs);
                    Quaternion terrainRotation = Quaternion.FromToRotation(Vector3.up, blendedNormal);
                    Quaternion finalRotation = terrainRotation * selectedPrefab.transform.rotation;

                    GameObject obj = Instantiate(selectedPrefab, spawnPos, finalRotation);
                    spawnedPositions.Add(spawnPos);

                    float normalizedNoise = Mathf.InverseLerp(threshold, 1f, noise);
                    normalizedNoise = Mathf.Pow(normalizedNoise, 0.7f);

                    float finalScale = Mathf.Lerp(minScale, maxScale, normalizedNoise);

                    Vector3 baseScale = selectedPrefab.transform.localScale;
                    obj.transform.localScale = baseScale * finalScale;

                    int randSeed = Random.Range(0, 5);
                    if (randSeed > 2 && prefabsAround != null && prefabsAround.Length > 0)
                    {
                        float radius = 8f;
                        float angle = Random.Range(0f, Mathf.PI * 2f);
                        float distance = Random.Range(6f, radius);

                        spawnPos.y += 0.5f;

                        float offsetX = Mathf.Cos(angle) * distance;
                        float offsetZ = Mathf.Sin(angle) * distance;

                        Vector3 randomPos = spawnPos + new Vector3(offsetX, 0f, offsetZ);
                        randomPos.y = terrain.SampleHeight(randomPos);

                        float normAroundX = (x + offsetX) / terrainSize.x;
                        float normAroundZ = (z + offsetZ) / terrainSize.z;
                        Vector3 terrainNormal2 = data.GetInterpolatedNormal(normAroundX, normAroundZ);
                        Vector3 blendedNormal2 = Vector3.Slerp(Vector3.up, terrainNormal2, terrainAlignment).normalized;

                        GameObject selectedPrefabAround = GetRandomPrefab(prefabsAround);
                        Quaternion terrainRotation2 = Quaternion.FromToRotation(Vector3.up, blendedNormal2);
                        Quaternion finalRotation2 = terrainRotation2 * selectedPrefabAround.transform.rotation;

                        GameObject obj2 = Instantiate(selectedPrefabAround, randomPos, finalRotation2);

                        float finalScale2 = Mathf.Lerp(minScale2, maxScale2, normalizedNoise);

                        Vector3 baseScale2 = selectedPrefabAround.transform.localScale;
                        obj2.transform.localScale = baseScale2 * finalScale2;
                    }
                }
            }
        }
    }
}