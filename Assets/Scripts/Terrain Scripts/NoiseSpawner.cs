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

    [Header("Granice Generowania")]
    [Tooltip("Margines od krawędzi terenu dla wież i dodatkowych obiektów")]
    public float terrainPadding = 50f;

    [Header("Główne Obiekty")]
    public SpawnItem[] prefabs;
    public int density = 5000;
    public float scale = 20f;
    public float threshold = 0.4f;
    public float minScale = 6.0f;
    public float maxScale = 10.0f;

    [Header("Obiekty wokół")]
    public SpawnItem[] prefabsAround;
    public float minScale2 = 2.0f;
    public float maxScale2 = 4.0f;

    [Header("Dodatkowe Obiekty (Szanują padding)")]
    public SpawnItem[] extraObjectPrefabs;
    public int extraObjectDensity = 500;
    public float extraObjectMinDistance = 5.0f;

    [Header("Wieże Strażnicze (Szanują padding)")]
    public GameObject watchtowerPrefab;
    public int watchtowerCount = 4;

    [Tooltip("Minimalny dystans między jedną wieżą a drugą")]
    public float watchtowerMinDistance = 100f;

    [Tooltip("Promień wokół wieży, w którym NIE będą rosnąć drzewa ani inne obiekty")]
    public float watchtowerClearance = 25f;

    [Tooltip("O ile wpuścić wieżę w ziemię, aby nie lewitowała na zboczach (wartość ujemna = głębiej w ziemię)")]
    public float watchtowerYOffset = -1.0f;

    [Tooltip("Jeśli zaznaczone, wieża zawsze będzie stać prosto pionowo, ignorując nachylenie stoku.")]
    public bool keepWatchtowerUpright = true;

    [Header("Ustawienia Fizyki i Rozmieszczenia")]
    public float minDistance = 15.0f;
    public float checkRadius = 1.5f;
    public LayerMask noSpawnLayer;
    [Range(0f, 1f)] public float terrainAlignment = 0.5f;

    [Header("UI Ekranu Ladowania")]
    public GameObject loadingScreen;
    public Slider loadingBar;
    public GameTimer gameTimer;

    private float totalWeight;
    private float totalWeightAround;
    private float totalWeightExtraObjects;
    private float cellSize;

    private Dictionary<Vector2Int, List<Vector3>> spatialGrid = new Dictionary<Vector2Int, List<Vector3>>();
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private List<Vector3> watchtowerPositions = new List<Vector3>();

    void Awake()
    {
        totalWeight = CalculateTotalWeight(prefabs);
        totalWeightAround = CalculateTotalWeight(prefabsAround);
        totalWeightExtraObjects = CalculateTotalWeight(extraObjectPrefabs);

        cellSize = minDistance;

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
        float extraObjMinDistSqr = extraObjectMinDistance * extraObjectMinDistance;
        float watchtowerMinDistSqr = watchtowerMinDistance * watchtowerMinDistance;
        float clearanceDistSqr = watchtowerClearance * watchtowerClearance;

        int spawnedThisFrame = 0;
        int totalObjectsToSpawn = watchtowerCount + density + extraObjectDensity;
        int currentSpawnedTotal = 0;

        for (int i = 0; i < watchtowerCount; i++)
        {
            if (watchtowerPrefab == null) break;

            for (int attempt = 0; attempt < 200; attempt++)
            {
                float x = Random.Range(terrainPadding, terrainSize.x - terrainPadding);
                float z = Random.Range(terrainPadding, terrainSize.z - terrainPadding);

                float worldX = terrainPosition.x + x;
                float worldZ = terrainPosition.z + z;
                float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));

                Vector3 towerPos = new Vector3(worldX, y + watchtowerYOffset, worldZ);
                Vector3 checkPos = new Vector3(worldX, y, worldZ);

                if (!Physics.CheckSphere(checkPos, checkRadius, noSpawnLayer) && !IsTooClose(checkPos, treeMinDistSqr))
                {
                    bool tooCloseToAnotherTower = false;
                    foreach (Vector3 existingTower in watchtowerPositions)
                    {
                        if ((towerPos - existingTower).sqrMagnitude < watchtowerMinDistSqr)
                        {
                            tooCloseToAnotherTower = true;
                            break;
                        }
                    }

                    if (!tooCloseToAnotherTower)
                    {
                        AddToGrid(towerPos);
                        watchtowerPositions.Add(towerPos);

                        Quaternion rotation;

                        if (keepWatchtowerUpright)
                        {
                            rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * watchtowerPrefab.transform.rotation;
                        }
                        else
                        {
                            float normX = (towerPos.x - terrainPosition.x) / terrainSize.x;
                            float normZ = (towerPos.z - terrainPosition.z) / terrainSize.z;
                            Vector3 terrainNormal = data.GetInterpolatedNormal(normX, normZ);
                            Vector3 blendedNormal = Vector3.Slerp(Vector3.up, terrainNormal, terrainAlignment).normalized;
                            rotation = Quaternion.FromToRotation(Vector3.up, blendedNormal) * watchtowerPrefab.transform.rotation;
                        }

                        GameObject obj = Instantiate(watchtowerPrefab, towerPos, rotation);
                        obj.SetActive(false);
                        spawnedObjects.Add(obj);

                        break;
                    }
                }
            }
            currentSpawnedTotal++;
            UpdateLoadingBar(currentSpawnedTotal, totalObjectsToSpawn);
        }
        for (int i = 0; i < density; i++)
        {
            currentSpawnedTotal++;
            UpdateLoadingBar(currentSpawnedTotal, totalObjectsToSpawn);

            float x = Random.Range(0, terrainSize.x);
            float z = Random.Range(0, terrainSize.z);

            float worldX = terrainPosition.x + x;
            float worldZ = terrainPosition.z + z;

            float noise = Mathf.PerlinNoise(worldX / terrainSize.x * scale, worldZ / terrainSize.z * scale);

            if (noise > threshold)
            {
                float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
                Vector3 spawnPos = new Vector3(worldX, y, worldZ);

                if (!Physics.CheckSphere(spawnPos, checkRadius, noSpawnLayer)
                    && !IsTooClose(spawnPos, treeMinDistSqr)
                    && !IsInClearanceZone(spawnPos, clearanceDistSqr))
                {
                    AddToGrid(spawnPos);
                    SpawnObject(prefabs, totalWeight, spawnPos, noise, data, terrainSize, terrainPosition, minScale, maxScale);

                    TrySpawnAround(spawnPos, noise, data, terrainSize, terrainPosition, clearanceDistSqr);
                    spawnedThisFrame++;
                }
            }

            if (spawnedThisFrame >= 50) { spawnedThisFrame = 0; yield return null; }
        }

        for (int i = 0; i < extraObjectDensity; i++)
        {
            currentSpawnedTotal++;
            UpdateLoadingBar(currentSpawnedTotal, totalObjectsToSpawn);

            float x = Random.Range(terrainPadding, terrainSize.x - terrainPadding);
            float z = Random.Range(terrainPadding, terrainSize.z - terrainPadding);

            float worldX = terrainPosition.x + x;
            float worldZ = terrainPosition.z + z;

            float y = terrain.SampleHeight(new Vector3(worldX, 0, worldZ));
            Vector3 objPos = new Vector3(worldX, y, worldZ);

            if (!Physics.CheckSphere(objPos, checkRadius, noSpawnLayer)
                && !IsTooClose(objPos, extraObjMinDistSqr)
                && !IsInClearanceZone(objPos, clearanceDistSqr))
            {
                AddToGrid(objPos);
                SpawnObject(extraObjectPrefabs, totalWeightExtraObjects, objPos, 0.5f, data, terrainSize, terrainPosition, minScale2, maxScale2);
                spawnedThisFrame++;
            }

            if (spawnedThisFrame >= 50) { spawnedThisFrame = 0; yield return null; }
        }

        foreach (var obj in spawnedObjects) obj.SetActive(true);

        if (loadingBar != null) loadingBar.value = 1f;
        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (gameTimer != null) gameTimer.StartTimer();

        spatialGrid.Clear();
        watchtowerPositions.Clear();
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
    private void TrySpawnAround(Vector3 spawnPos, float noise, TerrainData data, Vector3 terrainSize, Vector3 terrainPosition, float clearanceDistSqr)
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

            if (!Physics.CheckSphere(randomPos, checkRadius, noSpawnLayer) && !IsInClearanceZone(randomPos, clearanceDistSqr))
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
        if (items == null || items.Length == 0) return null;

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
    bool IsInClearanceZone(Vector3 pos, float clearanceDistSqr)
    {
        Vector2 pos2D = new Vector2(pos.x, pos.z);

        foreach (var towerPos in watchtowerPositions)
        {
            Vector2 towerPos2D = new Vector2(towerPos.x, towerPos.z);
            if ((pos2D - towerPos2D).sqrMagnitude < clearanceDistSqr) return true;
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
        if (loadingBar != null && total > 0)
            loadingBar.value = (float)current / total;
    }
}