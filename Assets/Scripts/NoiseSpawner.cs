using System.Collections.Generic;
using UnityEngine;

public class NoiseSpawner : MonoBehaviour
{
    public Terrain terrain;
    public GameObject[] prefab;
    public GameObject prefabAround;
    public int density = 5000;
    public float scale = 20f;
    public float threshold = 0.4f;
    public float minScale = 6.0f;
    public float maxScale = 10.0f;
    public float minScale2 = 4.0f;
    public float maxScale2 = 8.0f;
    public float minDistance = 15.0f;

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        Spawn();
    }

    void Spawn()
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainSize = data.size;
        Vector3 terrainPosition = terrain.transform.position;

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
                    int randomPrefabIndex = Random.Range(0, prefab.Length);
                    GameObject selectedPrefab = prefab[randomPrefabIndex];

                    GameObject obj = Instantiate(selectedPrefab, spawnPos, selectedPrefab.transform.rotation);
                    spawnedPositions.Add(spawnPos);

                    float normalizedNoise = Mathf.InverseLerp(threshold, 1f, noise);
                    normalizedNoise = Mathf.Pow(normalizedNoise, 0.7f);

                    float finalScale = Mathf.Lerp(minScale, maxScale, normalizedNoise);

                    Vector3 baseScale = selectedPrefab.transform.localScale;
                    obj.transform.localScale = baseScale * finalScale;

                    int randSeed = Random.Range(0, 5);
                    if (randSeed > 2)
                    {
                        float radius = 8f;
                        float angle = Random.Range(0f, Mathf.PI * 2f);
                        float distance = Random.Range(6f, radius);

                        spawnPos.y += 0.5f;

                        Vector3 randomPos = spawnPos + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

                        randomPos.y = terrain.SampleHeight(randomPos);

                        GameObject obj2 = Instantiate(prefabAround, randomPos, prefabAround.transform.rotation);

                        float finalScale2 = Mathf.Lerp(minScale2, maxScale2, normalizedNoise);

                        Vector3 baseScale2 = prefabAround.transform.localScale;
                        obj2.transform.localScale = baseScale2 * finalScale2;
                    }
                }
            }
        }
    }
}