using UnityEngine;

public class TerrainNoiseRandomizer : MonoBehaviour
{
    public Terrain terrain;
    public float scale = 1.03f;
    public float heightScale = 1.0f;
    public float scale2 = 4.9f;
    public float scale3 = 0.25f;
    public float scale4 = 0.39f;
    public float scale5 = 0.33f;

    void Start()
    {
        Generate();
    }

    void Generate()
    {
        TerrainData data = terrain.terrainData;
        int width = data.heightmapResolution;
        int height = data.heightmapResolution;

        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                float noise1 = Mathf.PerlinNoise(xCoord, yCoord);
                float noise2 = Mathf.PerlinNoise(xCoord * scale * scale2, yCoord * scale * scale2) * scale3;
                float combined = noise1 * scale4 + noise2 * scale5;
                heights[x, y] = combined * heightScale;
            }
        }

        data.SetHeights(0, 0, heights);
    }
}