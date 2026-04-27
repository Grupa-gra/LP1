using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SpawnItem
{
    public GameObject prefab;
    public float rarity = 10f;
}

public class NoiseSpawner : MonoBehaviour
{
    public Terrain terrain;

    [Header("Główne Obiekty (np. Drzewa)")]
    public SpawnItem[] prefabs;
    public int density = 5000;
    public float scale = 20f;
    public float threshold = 0.4f;
    public float minScale = 6.0f;
    public float maxScale = 10.0f;

    [Header("Obiekty wokół (np. Grzyby)")]
    public SpawnItem[] prefabsAround;
    public float minScale2 = 2.0f;
    public float maxScale2 = 4.0f;

    [Header("Nowa Sekcja: Pniaki")]
    public SpawnItem[] stumpPrefabs;
    public int stumpDensity = 500;
    public float stumpMinDistance = 5.0f; // Mniejszy dystans specjalnie dla pniaków!

    [Header("Ustawienia Fizyki i Rozmieszczenia")]
    public float minDistance = 15.0f; // To obowiązuje tylko duże drzewa
    public float checkRadius = 1.5f;
    public LayerMask noSpawnLayer;
    [Range(0f, 1f)] public float terrainAlignment = 0.5f;

    [Header("UI Ekranu Ladowania")]
    public GameObject loadingScreen;
    public Slider loadingBar;
    public GameTimer gameTimer;

    private float totalWeight;
    private float totalWeightAround;
    private float totalWeightStumps;
    private float cellSize;

    private Dictionary<Vector2Int, List<Vector3>> spatialGrid = new Dictionary<Vector2Int, List<Vector3>>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Awake()
    {
        totalWeight = CalculateTotalWeight(prefabs);
        totalWeightAround = CalculateTotalWeight(prefabsAround);
        totalWeightStumps = CalculateTotalWeight(stumpPrefabs);

        cellSize = minDistance; // Rozmiar siatki robimy pod największe obiekty

        if (loadingScreen != null) loadingScreen.SetActive(true);
        if (loadingBar != null) loadingBar.value = 0f;
    }

    void Start()
    {
        StartCoroutine(SpawnCoroutine());
    }

    IEnumerator SpawnCoroutine()
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainSize = data.size;
        Vector3 terrainPosition = terrain.transform.position;
        float treeMinDistSqr = minDistance * minDistance;
        float stumpMinDistSqr = stumpMinDistance * stumpMinDistance;
        int spawnedThisFrame = 0;

        // --- KROK 1: SPAWNOWANIE DRZEW I GRZYBÓW ---
        for (int i = 0; i < density; i++)
        {
            UpdateLoadingBar(i, density + stumpDensity);

            float x = Random.Range(0, terrainSize.x);
            float z = Random.Range(0, terrainSize.z);
            float worldX = terrainPosition.x + x;
            float worldZ = terrainPosition.z + z;

            float noise = Mathf.PerlinNoise(worldX / terrainSize.x * scale, worldZ / terrainSize.z * scale);

            if (noise > threshold)
            {
                float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
                Vector3 spawnPos = new Vector3(worldX, y, worldZ);

                // Drzewa używają gigantycznego dystansu (minDistance)
                if (!Physics.CheckSphere(spawnPos, checkRadius, noSpawnLayer) && !IsTooClose(spawnPos, treeMinDistSqr))
                {
                    AddToGrid(spawnPos);
                    SpawnObject(prefabs, totalWeight, spawnPos, noise, data, terrainSize, terrainPosition, minScale, maxScale);

                    TrySpawnAround(spawnPos, noise, data, terrainSize, terrainPosition);
                    spawnedThisFrame++;
                }
            }

            if (spawnedThisFrame >= 50) { spawnedThisFrame = 0; yield return null; }
        }

        // --- KROK 2: SPAWNOWANIE PNIAKÓW ---
        for (int i = 0; i < stumpDensity; i++)
        {
            UpdateLoadingBar(density + i, density + stumpDensity);

            float x = Random.Range(0, terrainSize.x);
            float z = Random.Range(0, terrainSize.z);
            float worldX = terrainPosition.x + x;
            float worldZ = terrainPosition.z + z;

            float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
            Vector3 stumpPos = new Vector3(worldX, y, worldZ);

            // Pniaki używają mniejszego dystansu (stumpMinDistance)
            if (!Physics.CheckSphere(stumpPos, checkRadius, noSpawnLayer) && !IsTooClose(stumpPos, stumpMinDistSqr))
            {
                AddToGrid(stumpPos);
                SpawnObject(stumpPrefabs, totalWeightStumps, stumpPos, 0.5f, data, terrainSize, terrainPosition, minScale2, maxScale2);
                spawnedThisFrame++;
            }

            if (spawnedThisFrame >= 50) { spawnedThisFrame = 0; yield return null; }
        }

        // --- FINALIZACJA ---
        foreach (var obj in spawnedObjects) obj.SetActive(true);

        if (loadingBar != null) loadingBar.value = 1f;
        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (gameTimer != null) gameTimer.StartTimer();

        spatialGrid.Clear();
        spawnedObjects.Clear();
    }

    private void SpawnObject(SpawnItem[] pool, float weight, Vector3 pos, float noiseValue, TerrainData data, Vector3 tSize, Vector3 tPos, float minS, float maxS)
    {
        if (pool == null || pool.Length == 0) return;

        float normX = (pos.x - tPos.x) / tSize.x;
        float normZ = (pos.z - tPos.z) / tSize.z;

        Vector3 terrainNormal = data.GetInterpolatedNormal(normX, normZ);
        Vector3 blendedNormal = Vector3.Slerp(Vector3.up, terrainNormal, terrainAlignment).normalized;

        GameObject selectedPrefab = GetRandomPrefab(pool, weight);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, blendedNormal) * selectedPrefab.transform.rotation;

        GameObject obj = Instantiate(selectedPrefab, pos, rotation);
        obj.SetActive(false);

        float normalizedNoise = Mathf.InverseLerp(threshold, 1f, noiseValue);
        float finalScale = Mathf.Lerp(minS, maxS, normalizedNoise);
        obj.transform.localScale = selectedPrefab.transform.localScale * finalScale;

        spawnedObjects.Add(obj);
    }

    private void TrySpawnAround(Vector3 spawnPos, float noise, TerrainData data, Vector3 terrainSize, Vector3 terrainPosition)
    {
        if (prefabsAround == null || prefabsAround.Length == 0) return;

        int randSeed = Random.Range(0, 5);
        if (randSeed > 2)
        {
            float radius = 8f;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(6f, radius);

            Vector3 randomPos = spawnPos + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            randomPos.y = terrain.SampleHeight(randomPos);

            // Wywaliłem stąd blokadę odległości (IsTooClose) - grzyby rosną blisko jak dawniej!
            if (!Physics.CheckSphere(randomPos, checkRadius, noSpawnLayer))
            {
                AddToGrid(randomPos);
                SpawnObject(prefabsAround, totalWeightAround, randomPos, noise, data, terrainSize, terrainPosition, minScale2, maxScale2);
            }
        }
    }

    private float CalculateTotalWeight(SpawnItem[] items)
    {
        if (items == null) return 0;
        float total = 0;
        foreach (var item in items) total += item.rarity;
        return total;
    }

    GameObject GetRandomPrefab(SpawnItem[] items, float weight)
    {
        float randomValue = Random.Range(0f, weight);
        float currentWeight = 0f;

        foreach (var item in items)
        {
            currentWeight += item.rarity;
            if (randomValue <= currentWeight) return item.prefab;
        }
        return items[0].prefab;
    }

    bool IsTooClose(Vector3 pos, float minDistSqr)
    {
        Vector2Int cell = GetCell(pos);
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Vector2Int neighbor = new Vector2Int(cell.x + x, cell.y + z);
                if (spatialGrid.TryGetValue(neighbor, out var list))
                {
                    foreach (var other in list)
                    {
                        if ((pos - other).sqrMagnitude < minDistSqr) return true;
                    }
                }
            }
        }
        return false;
    }

    void AddToGrid(Vector3 pos)
    {
        Vector2Int cell = GetCell(pos);
        if (!spatialGrid.ContainsKey(cell)) spatialGrid[cell] = new List<Vector3>();
        spatialGrid[cell].Add(pos);
    }

    Vector2Int GetCell(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / cellSize), Mathf.FloorToInt(pos.z / cellSize));
    }

    void UpdateLoadingBar(int current, int total)
    {
        if (loadingBar != null)
            loadingBar.value = (float)current / total;
    }
}