using UnityEngine;

public class TerrainTextureMixer : MonoBehaviour
{
    public Terrain terrain;

    [Header("Textures")]
    public Texture2D textureA;   // np. trawa
    public Texture2D textureB;   // np. skała

    [Header("Noise Settings")]
    public float scale = 5f;
    public float threshold = 0.5f;

    void Start()
    {
        ApplyTextures();
    }

    void ApplyTextures()
    {
        TerrainData data = terrain.terrainData;

        // --- 1️⃣ Tworzymy Terrain Layers ---
        TerrainLayer layerA = new TerrainLayer();
        layerA.diffuseTexture = textureA;
        layerA.tileSize = new Vector2(15, 15);

        TerrainLayer layerB = new TerrainLayer();
        layerB.diffuseTexture = textureB;
        layerB.tileSize = new Vector2(15, 15);

        data.terrainLayers = new TerrainLayer[] { layerA, layerB };

        // --- 2️⃣ Tworzymy alphamapę (splatmap) ---
        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        int layers = data.alphamapLayers;

        float[,,] alphamaps = new float[width, height, layers];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float normX = (float)x / width;
                float normY = (float)y / height;

                float noise = Mathf.PerlinNoise(normX * scale, normY * scale);

                // Płynne przejście
                float blend = Mathf.SmoothStep(threshold - 0.1f, 
                    threshold + 0.1f, 
                    noise);

                alphamaps[x, y, 0] = 1 - blend; // tekstura A
                alphamaps[x, y, 1] = blend;     // tekstura B
            }
        }

        data.SetAlphamaps(0, 0, alphamaps);
    }
}