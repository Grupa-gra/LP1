using UnityEngine;

public class TerrainLayerRandomizer : MonoBehaviour
{
    public Terrain terrain;

    [Header("Randomization Settings")] public int layerIndex = 0; // który TerrainLayer chcesz randomizować
    public float noiseScale = 5f; // jak duże plamy
    public float noiseStrength = 2.0f; // jak bardzo modyfikować

    void Start()
    {
        RandomizeLayer();
    }

    void RandomizeLayer()
    {
        TerrainData data = terrain.terrainData;

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        int layers = data.alphamapLayers;
        float[,,] alphamaps = data.GetAlphamaps(0, 0, width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int l = 0; l < layers; l++)
                {
                    alphamaps[x, y, l] = (l == 0 ? 1f : 0f);
                }
            }
        }

        terrain.terrainData.SetAlphamaps(0, 0, alphamaps);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float normX = (float)x / width;
                float normY = (float)y / height;

                // noise 0–1
                float noise = Mathf.PerlinNoise(normX * noiseScale, normY * noiseScale);

                // zmodyfikuj warstwę
                alphamaps[x, y, layerIndex] += (noise - 0.5f) * noiseStrength;

                // ograniczamy w przedziale 0–1
                alphamaps[x, y, layerIndex] = Mathf.Clamp01(alphamaps[x, y, layerIndex]);
            }
        }

        data.SetAlphamaps(0, 0, alphamaps);
    }
}