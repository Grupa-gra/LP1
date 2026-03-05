using UnityEngine;

public class TerrainAutoTexture : MonoBehaviour
{
    public Terrain terrain;

    public float rockSlope = 35f;
    public float noiseScale = 15f;

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

                float slope = terrainData.GetSteepness(normX, normY);

                float[] weights = new float[layers];

                float noiseA = Mathf.PerlinNoise(normX * noiseScale, normY * noiseScale);
                float noiseB = Mathf.PerlinNoise(normY * noiseScale, normX * noiseScale);

                if (slope > rockSlope)
                {
                    if (layers > 2)
                    {
                        weights[2] = 1f;
                    }
                }
                else
                {
                    if (layers > 0)
                    {
                        weights[0] = noiseA;
                    }
                    if (layers > 1)
                    {
                        weights[1] = noiseB;
                    }
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