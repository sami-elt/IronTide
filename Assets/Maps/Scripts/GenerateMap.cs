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

    [Header("Glow Border Settings")]
    public Material glowMaterial;
    public float borderWidth = 0.4f;
    public int playerCount = 2;

    [Header("Wall Border")]
    public GameObject wallPrefab;
    //public float wallHeight = 2f;
    public float wallDistance = 1.2f;
    public float wallYOffset = 10f;
    public int wallsPerSide = 2;

    [Header("WaveSystem")]
    public ParticleSystem magicBarrierPrefab; 
    private ParticleSystem currentBarrier;
    public GameObject magicBarrierObject;

    Vector2Int[] spawnPoints;
    public Vector3[] spawnWorldPositions;

    Vector2Int[] allSpawnPoints = new Vector2Int[]
    {
        new Vector2Int(-8, 15), // top
        new Vector2Int(8, -15), // down
        new Vector2Int(15, 0), // right
        new Vector2Int(-15, 0) // left
    };

    void Start()
    {
        GenerateFullMap();
    }

    //  KÖR ALLT I RÄTT ORDNING
    public void GenerateFullMap()
    {
        SetupSpawnPoints();
        ClearMap();
        Generate();
        SpawnPlayerStarts();
        GenerateGlowBorder();
        GenerateWallBorder();

        Vector3[] corners = GetOuterCorners();
        UpdateMagicBarrier(corners);
        // säg till andra system att 
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
    // 🔥 UPPDATERAD: GENERERA DEN LYSANDE KANTEN I RÄTT VINKEL
    // 🔥 EN HELT NY OCH SKOTTSÄKER METOD: RITA UTIFRÅN DE FAKTISKA PLATTERNA
    void GenerateGlowBorder()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = gameObject.AddComponent<LineRenderer>();
        }

        lr.startWidth = borderWidth;
        lr.endWidth = borderWidth;
        lr.alignment = LineAlignment.View;
        lr.sharedMaterial = glowMaterial;
        lr.loop = true;

        // Vi skapar en lista för att hitta de 6 yttersta hörn-positionerna
        Vector3[] corners = new Vector3[6];

        // Vi räknar ut hörnen baserat på kartans faktiska max-dimensioner i din HexToWorld-matematik
        // Genom att hämta positionerna direkt från dina egna formler blir det 100% rätt vinkel.
        corners[0] = HexToWorld(0, radius);           // Högst upp
        corners[1] = HexToWorld(radius, 0);           // Uppe till höger
        corners[2] = HexToWorld(radius, -radius);     // Nere till höger
        corners[3] = HexToWorld(0, -radius);          // Längst ner
        corners[4] = HexToWorld(-radius, 0);          // Nere till vänster
        corners[5] = HexToWorld(-radius, radius);     // Uppe till vänster

        // Justera ut linjen en liten bit så den inte skär rakt igenom de yttersta plattorna
        // Vi flyttar varje hörn lite grann utåt från kartans mittpunkt (0,0,0)
        float margin = 1.05f; // Öka detta värde om du vill ha kanten längre ut
        for (int i = 0; i < 6; i++)
        {
            corners[i].x *= margin;
            corners[i].z *= margin;

            // Vi sätter Y till 0.5f så att linjen svävar snyggt OVANFÖR vattnet och inte drunknar
            corners[i].y = 0.5f;
        }

        // Applicera positionerna på din Line Renderer
        lr.positionCount = 6;
        lr.SetPositions(corners);
    }


    // Denna funktion räknar ut exakt var de 6 hörnen på din hexagon är
    Vector3[] GetOuterCorners()
    {
        Vector3[] corners = new Vector3[6];

        // Vi använder din existerande HexToWorld-logik för att hitta hörnen
        corners[0] = HexToWorld(0, radius);           // Högst upp
        corners[1] = HexToWorld(radius, 0);           // Uppe till höger
        corners[2] = HexToWorld(radius, -radius);     // Nere till höger
        corners[3] = HexToWorld(0, -radius);          // Längst ner
        corners[4] = HexToWorld(-radius, 0);          // Nere till vänster
        corners[5] = HexToWorld(-radius, radius);     // Uppe till vänster

        // Justera ut hörnen lite så de ligger utanför kartan
        float margin = 1.05f;
        for (int i = 0; i < 6; i++)
        {
            corners[i].x *= margin;
            corners[i].z *= margin;
            corners[i].y = 0.5f; // Sätt höjden så de syns tydligt
        }

        return corners;
    }

    void GenerateWallBorder()
    {
        Vector3[] corners = new Vector3[6];

        corners[0] = HexToWorld(0, radius);
        corners[1] = HexToWorld(radius, 0);
        corners[2] = HexToWorld(radius, -radius);
        corners[3] = HexToWorld(0, -radius);
        corners[4] = HexToWorld(-radius, 0);
        corners[5] = HexToWorld(-radius, radius);

        // Flytta ut kanten lite
        for (int i = 0; i < 6; i++)
        {
            corners[i].x *= wallDistance;
            corners[i].z *= wallDistance;
        }

        // Spawn walls
        for (int i = 0; i < 6; i++)
        {
            Vector3 start = corners[i];
            Vector3 end = corners[(i + 1) % 6];

            Vector3 direction = (end - start).normalized;

            for (int j = 0; j < wallsPerSide; j++)
            {
                // Skip corners
                float t = (j + 1f) / (wallsPerSide + 1f);

                Vector3 pos = Vector3.Lerp(start, end, t);

                pos.y = wallYOffset;

                GameObject wall =
                    Instantiate(wallPrefab, pos, Quaternion.identity);

                wall.transform.rotation =
                    Quaternion.LookRotation(direction);

                wall.transform.SetParent(transform);
            }
        }
    }


    void UpdateMagicBarrier(Vector3[] corners)
    {
        if (magicBarrierObject == null) return;

        // Aktivera effekten
        magicBarrierObject.SetActive(true);

        // Hitta avståndet till hörnet för att skala rätt
        float distanceToCorner = Vector3.Distance(Vector3.zero, corners[0]);

        // Justera storleken på partikelsystemets form så den matchar kartan
        var ps = magicBarrierObject.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var shape = ps.shape;
            shape.radius = distanceToCorner * 1.15f; // Lite utanför muren
        }
    }
    public void SetMagicIntensity(float intensity)
    {
        var renderer = magicBarrierObject.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            // Förutsatt att ditt material har en "GlowIntensity" parameter
            renderer.material.SetFloat("_GlowIntensity", intensity);
        }
    }
    //void GenerateMagicBarrier(Vector3[] corners)
    //{
    //    if (magicBarrierObject == null) return;

    //    // 1. Gör det till ett "barn" till din World-map så att det följer med
    //    magicBarrierObject.transform.SetParent(this.transform);

    //    // 2. Flytta det till mitten (0,0,0 lokalt)
    //    magicBarrierObject.transform.localPosition = new Vector3(0, 0.5f, 0); // 0.5f för att hamna lite ovanför vattnet

    //    // 3. Konfigurera partikelsystemet som finns PÅ objektet
    //    ParticleSystem ps = magicBarrierObject.GetComponent<ParticleSystem>();
    //    if (ps != null)
    //    {
    //        var shape = ps.shape;
    //        shape.shapeType = ParticleSystemShapeType.Circle;
    //        shape.radius = radius * 1.3f; // Anpassa storleken
    //        shape.rotation = new Vector3(90, 0, 0);
    //    }
    //}

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
