using UnityEngine;

public class TerrainAutoTexture : MonoBehaviour
{
    public Terrain terrain;

    [Header("Slope Blending (Ska³y)")]
    public float rockSlope = 35f;
    public float rockBlendRange = 15f;

    [Header("Noise Settings (P³askie tekstury)")]
    public float noiseScale = 15f;
    public float noiseOffset = 500f;

    [Header("Warp Settings (Zniekszta³cenia)")]
    public float warpScale = 8f;
    public float warpAmount = 0.08f;

    void Start()
    {
        ApplyTextures();
    }

    void ApplyTextures()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        if (terrain == null) return;

        TerrainData terrainData = terrain.terrainData;

        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        int layers = terrainData.alphamapLayers;

        float[,,] splatmap = new float[height, width, layers];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normX = (float)x / (width - 1);
                float normY = (float)y / (height - 1);

                float warpX = Mathf.PerlinNoise(normX * warpScale, normY * warpScale) * warpAmount;
                float warpY = Mathf.PerlinNoise(normX * warpScale + 100f, normY * warpScale + 100f) * warpAmount;

                float warpedX = normX + warpX;
                float warpedY = normY + warpY;

                float slope = terrainData.GetSteepness(normX, normY);
                float[] weights = new float[layers];

                float noiseA = Mathf.PerlinNoise(warpedX * noiseScale, warpedY * noiseScale);
                float noiseB = Mathf.PerlinNoise(warpedX * noiseScale + noiseOffset, warpedY * noiseScale + noiseOffset);

                float slopeFactor = Mathf.InverseLerp(rockSlope, rockSlope + rockBlendRange, slope);
                slopeFactor = Mathf.SmoothStep(0f, 1f, slopeFactor);
                float flatFactor = 1f - slopeFactor;

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