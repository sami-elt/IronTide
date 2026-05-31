using NueGames.NueDeck.Scripts.Managers;
using UnityEngine;

[ExecuteAlways]
public class GenerateMap : MonoBehaviour
{
    [Header("Map")]
    public int radius = 10;
    public float hexSize = 1f;

    [Header("Prefabs")]
    public GameObject waterPrefab;
    public GameObject rockPrefab;
    public GameObject hillPrefab;
    public GameObject spawnPointPrefab;

    public int playerCount = 2;

    Vector2Int[] spawnPoints;
    public Vector3[] spawnWorldPositions;

    Vector2Int[] allSpawnPoints = new Vector2Int[]
    {
        new Vector2Int(-5, 10),
        new Vector2Int(5, -10),
        new Vector2Int(10, 0),
        new Vector2Int(-10, 0)
    };

    void Start()
    {
        GenerateFullMap();
    }

    // 🔥 KÖR ALLT I RÄTT ORDNING
    public void GenerateFullMap()
    {
        SetupSpawnPoints();
        ClearMap();
        Generate();
        SpawnPlayerStarts();

        // 🔥 säg till andra system att map är klar
        FindFirstObjectByType<GameManagerTest>()?.OnMapReady();
    }

    void SetupSpawnPoints()
    {
        if (playerCount == 2)
        {
            spawnPoints = new Vector2Int[]
            {
                allSpawnPoints[0],
                allSpawnPoints[1]
            };
        }
        else if (playerCount == 3)
        {
            spawnPoints = new Vector2Int[]
            {
                allSpawnPoints[0],
                allSpawnPoints[1],
                allSpawnPoints[2]
            };
        }
        else
        {
            spawnPoints = allSpawnPoints;
        }
    }

    void Generate()
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                int y = -x - z;

                if (Mathf.Abs(x) > radius || Mathf.Abs(z) > radius || Mathf.Abs(y) > radius)
                    continue;

                Vector3 pos = HexToWorld(x, z);

                GameObject prefab;

                if (IsSpawnPoint(x, z) || IsInSafeZone(x, z))
                {
                    prefab = waterPrefab;
                }
                else
                {
                    float rand = Random.value;

                    if (rand < 0.85f)
                        prefab = waterPrefab;
                    else if (rand < 0.93f)
                        prefab = hillPrefab;
                    else
                        prefab = rockPrefab;
                }

                Instantiate(prefab, pos, Quaternion.identity, transform);
            }
        }
    }

    bool IsSpawnPoint(int x, int z)
    {
        foreach (var sp in spawnPoints)
        {
            if (sp.x == x && sp.y == z)
                return true;
        }
        return false;
    }

    bool IsInSafeZone(int x, int z)
    {
        foreach (var sp in spawnPoints)
        {
            float dist = Vector2.Distance(new Vector2(x, z), new Vector2(sp.x, sp.y));

            if (dist < 3f)
                return true;
        }
        return false;
    }

    Vector3 HexToWorld(int x, int z)
    {
        float spacing = 1.05f;
        float xPos = hexSize * Mathf.Sqrt(3f) * (x + z * 0.5f);
        float zPos = hexSize * 1.5f * z;

        return new Vector3(xPos, 0, zPos) * spacing;
    }

    void SpawnPlayerStarts()
    {
        spawnWorldPositions = new Vector3[spawnPoints.Length];

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Vector2Int coord = spawnPoints[i];

            Vector3 pos = HexToWorld(coord.x, coord.y);
            spawnWorldPositions[i] = pos;

            Instantiate(spawnPointPrefab, pos + Vector3.up * 0.2f, Quaternion.identity, transform);
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
