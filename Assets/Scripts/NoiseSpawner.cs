using UnityEngine;

public class TerrainSpawner : MonoBehaviour
{
    public Terrain terrain;
    public GameObject prefab;
    public int density = 5000;
    public float scale = 20f;
    public float threshold = 0.4f;
    public float minScale = 2.0f;
    public float maxScale = 15.0f;

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

            float noise = Mathf.PerlinNoise(worldX / terrainSize.x * scale,
                worldZ / terrainSize.z * scale);

            if (noise > threshold)
            {
                float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
                Vector3 spawnPos = new Vector3(worldX, y, worldZ);

                GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

                float normalizedNoise = Mathf.InverseLerp(threshold, 1f, noise);
                normalizedNoise = Mathf.Pow(normalizedNoise, 0.7f);

                float finalScale = Mathf.Lerp(minScale, maxScale, normalizedNoise);

                Vector3 baseScale = prefab.transform.localScale;
                obj.transform.localScale = baseScale * finalScale;
            }
        }
    }
}