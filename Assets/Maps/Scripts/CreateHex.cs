using UnityEngine;
[ExecuteAlways]
public class CreateHex : MonoBehaviour
{
    public int width = 20;
    public int height = 20;

    [Header("Prefabs")]
    public GameObject hexPrefab;       //  water
    public GameObject mountainPrefab;  // blocker
    public GameObject hilPrefab;       // variation
    public GameObject grassPrefab;

    [Header("Spacing")]
    public float widthOffset = 20.5f;
    public float heightOffset = 17.5f;
    public float waterHeight = -1f;
    public float hillOffset = 1f;
    public float mountainOffset = 0.5f;

    private void Start()
    {
        ClearGrid();
        GenerateGrid();
    }
    public void GenerateGrid()
    {
        Debug.Log("Genrate Körs");

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = x * widthOffset;

                if (z % 2 == 1)
                    xPos += widthOffset / 2f;

                float zPos = z * heightOffset;

                float waterHeight = 0f; // justera denna!
                Vector3 position = new Vector3(xPos, waterHeight, zPos);
                Vector3 mountainPos = new Vector3(xPos, 5, zPos);
                Vector3 hillPos = new Vector3(xPos, 5, zPos);

                bool isEdge = x == 0 || z == 0 || x == width - 1 || z == height - 1;

                GameObject tileObject;
                TileType tileType;

                //  EDGE 
                if (isEdge)
                {

                    tileObject = Instantiate(grassPrefab, position, Quaternion.identity, transform);
                    tileType = TileType.Grass;


                }
                else
                {


                    tileObject = Instantiate(hexPrefab, position, Quaternion.identity, transform);
                    tileType = GetTileType(x, z);

                    // terrain ovanpå bara water
                    if (tileType == TileType.Mountain)
                    {
                        Instantiate(mountainPrefab, mountainPos, Quaternion.identity, tileObject.transform);

                    }
                    else if (tileType == TileType.Hill)
                    {
                        Instantiate(hilPrefab, hillPos, Quaternion.identity, tileObject.transform);
                    }

                    // setup tile
                    HexTile tile = tileObject.GetComponent<HexTile>();

                    if (tile != null)
                    {
                        tile.Setup(tileType);
                    }
                }
            }
        }
        TileType GetTileType(int x, int z)
        {
            float r = Random.value;

            // 90% water
            if (r < 0.9f)
                return TileType.Water;

            // 5% mountain
            if (Random.value < 0.5f)
                return TileType.Mountain;

            return TileType.Hill;
        }
       
    }
    void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}
