using UnityEngine;


public class GridManager : MonoBehaviour
{
    public int width = 18;
    public int height = 18;
    public GameObject tilePrefab;
    public GameObject rockPrefab;
    public int rockCount = 10;
    private Tile[,] grid;
    public Transform rockParent;

    [Header("Spacing")]
    public float tileSize = 0.5f;
    public bool generateGrid;

    void Update()
    {
        if (generateGrid)
        {
            generateGrid = false;
            GenerateGrid();
            SpawnRocks();
        }
        
    }

    void GenerateGrid()
    {
        ClearGrid();

        grid = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 position = new Vector3(
                    (x - width / 2f) * tileSize,
                    0.5f,
                    (z - height / 2f) * tileSize
                );

                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, transform);

                Tile tile = tileObject.GetComponent<Tile>();
                grid[x, z] = tile;
            }
        }
    }
    void SpawnRocks()
    {
        int spawned = 0;

        while (spawned < rockCount)
        {
            int randomX = Random.Range(0, width);
            int randomY = Random.Range(0, height);

            Tile tile = grid[randomX, randomY];

            if (!tile.isOccupied)
            {
                Instantiate(rockPrefab, tile.transform.position + Vector3.up * 0.2f, Quaternion.identity, rockParent);
                tile.isOccupied = true;
                spawned++;
            }
        }
    }

    void ClearGrid()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
}