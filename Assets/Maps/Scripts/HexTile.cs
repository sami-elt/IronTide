using UnityEngine;

public class HexTile : MonoBehaviour
{
    public TileType tileType;
    public bool isWalkable;

    public void Setup(TileType type)
    {
        tileType = type;

        switch (tileType)
        {
            case TileType.Water:
                isWalkable = true;
                break;

            case TileType.Mountain:
            case TileType.Hill:
            case TileType.Grass:
                isWalkable = false;
                break;
        }
    }

    void OnMouseDown()
    {
        Debug.Log("CLICKED TILE");

        if (!isWalkable)
            return;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            Debug.LogError("PLAYER NOT FOUND");
            return;
        }

        Debug.Log("MOVING PLAYER");

        player.MoveTo(new Vector3(transform.position.x, 1f, transform.position.z));
    }
}