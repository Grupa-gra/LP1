using UnityEngine;

public class TerrainAutoTexture : MonoBehaviour
{
    public Terrain terrain;

    public float rockSlope = 35f;
    public float rockBlendRange = 10f;

    public float noiseScale = 15f;
    public float noiseOffset = 500f;

    public float warpScale = 8f;
    public float warpAmount = 0.08f;

    void Start()
    {
        ApplyTextures();
    }

    void ApplyTextures()
    {
        TerrainData terrainData = terrain.terrainData;

        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layers = terrainData.alphamapLayers;

        float[,,] splatmap = new float[height, width, layers];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normX = x * 1.0f / width;
                float normY = y * 1.0f / height;

                float warpX = Mathf.PerlinNoise(normX * warpScale, normY * warpScale) * warpAmount;
                float warpY = Mathf.PerlinNoise(normX * warpScale + 100f, normY * warpScale + 100f) * warpAmount;

                float warpedX = normX + warpX;
                float warpedY = normY + warpY;

                float slope = terrainData.GetSteepness(normX, normY);
                float[] weights = new float[layers];

                float noiseA = Mathf.PerlinNoise(warpedX * noiseScale, warpedY * noiseScale);
                float noiseB = Mathf.PerlinNoise(warpedX * noiseScale + noiseOffset, warpedY * noiseScale + noiseOffset);

                float slopeFactor = Mathf.InverseLerp(rockSlope, rockSlope + rockBlendRange, slope);
                float flatFactor = 1f / (1f + slopeFactor * 100f);

                if (layers > 0)
                {
                    weights[0] = noiseA * flatFactor;
                }
                if (layers > 1)
                {
                    weights[1] = noiseB * flatFactor;
                }
                if (layers > 2)
                {
                    weights[2] = slopeFactor;
                }

                float total = 0f;
                for (int i = 0; i < layers; i++)
                {
                    total += weights[i];
                }

                for (int i = 0; i < layers; i++)
                {
                    if (total > 0f)
                    {
                        splatmap[y, x, i] = weights[i] / total;
                    }
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmap);
    }
}