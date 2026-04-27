using UnityEngine;

public class GenerateMap : MonoBehaviour
{
    [Header("Map")]
    public int radius = 10;
    public float hexSize = 1f;

    [Header("Prefabs")]
    public GameObject waterPrefab;
    public GameObject rockPrefab;
    public GameObject hillPrefab;
    public GameObject zonePrefab;

    [Header("Noise")]
    public float noiseScale = 0.1f;

    Vector2Int[] spawnPoints;

    void Start()
    {
        SetupSpawnPoints();
        ClearMap();
        Generate();
        SpawnZones();

    }

    void SetupSpawnPoints()
    {
        int r = radius;

        spawnPoints = new Vector2Int[]
        {
            new Vector2Int(0, r),
            new Vector2Int(r, -r),
            new Vector2Int(0, -r),
            new Vector2Int(-r, r)
        };
    }

    void Generate()
    {
        float spacing = 1.05f;

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                int y = -x - z;

                if (Mathf.Abs(x) > radius || Mathf.Abs(z) > radius || Mathf.Abs(y) > radius)
                    continue;

                float xPos = hexSize * Mathf.Sqrt(3f) * (x + z * 0.5f);
                float zPos = hexSize * 1.5f * z;

                Vector3 pos = new Vector3(xPos, 0, zPos) * spacing;

                GameObject prefab = waterPrefab;

                float noise = Mathf.PerlinNoise(
                    (x + 100) * noiseScale,
                    (z + 100) * noiseScale
                );

                // EXTRA LAGER 
                float noise2 = Mathf.PerlinNoise(
                    (x - 50) * noiseScale * 0.5f,
                    (z - 50) * noiseScale * 0.5f
                );

                float combined = (noise + noise2) * 0.5f;

                bool inCenter = IsInCenter(x, z);
                bool inSafeZone = IsInSafeZone(x, z);

               

                //  HINDER LOGIK 


                if (inSafeZone)
                {
                    prefab = waterPrefab;
                }
                else
                {
                    float rand = Random.value;

                    if (rand > 0.9f)        // 10%
                        prefab = rockPrefab;
                    else if (rand > 0.8f)   // 10%
                        prefab = hillPrefab;
                    else
                        prefab = waterPrefab;
                }
                //if (!inSafeZone)
                //{
                //    float rand = Random.value;

                //    if (rand > 0.97f)
                //        SpawnObstacle(rockPrefab, pos);

                //    //else if (rand > 0.85f)
                //    //    SpawnObstacle(hillPrefab, pos);
                //}

                Instantiate(prefab, pos, Quaternion.identity, transform);
            }
        }
        
    }

    bool IsInCenter(int x, int z)
    {
        float dist = Mathf.Sqrt(x * x + z * z);
        return dist < radius * 0.25f;
    }

    bool IsInSafeZone(int x, int z)
    {
        foreach (var spawn in spawnPoints)
        {
            float dist = Vector2.Distance(
                new Vector2(x, z),
                new Vector2(spawn.x, spawn.y)
            );

            if (dist < 4f) // zon storlek
                return true;
        }
        return false;
    }

    void SpawnObstacle(GameObject prefab, Vector3 basePos)
    {
        float height = 0.4f;

        Vector3 offset = new Vector3(0, height, 0);

        Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);

        GameObject obj = Instantiate(prefab, basePos + offset, rot, transform);

        // 🔥 viktig: random scale
        obj.transform.localScale *= Random.Range(0.8f, 1.2f);
    }

    Vector3 GetClosestTile(Vector3 target)
    {
        Vector3 closest = Vector3.zero;
        float minDist = Mathf.Infinity;

        foreach (Transform child in transform)
        {
            float dist = Vector3.Distance(child.position, target);
            if (dist < minDist)
            {
                minDist = dist;
                closest = child.position;
            }
        }

        return closest;
    }

    void SpawnZones()
    {
        float distance = radius * hexSize * 1.6f;

        Vector3[] directions = new Vector3[]
        {
            new Vector3(0, 0, 1),
            new Vector3(3, 0, 0),
            new Vector3(0, 0, -1),
            new Vector3(-3, 0, 0)
        };

        foreach (var dir in directions)
        {
            Vector3 target = dir * distance;
            Vector3 pos = GetClosestTile(target);

            Instantiate(zonePrefab, pos, Quaternion.identity);
        }
    }

    void ClearMap()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}