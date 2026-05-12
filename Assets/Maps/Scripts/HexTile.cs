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
        if (!isWalkable)
            return;

        if (TurnManager.Instance == null || TurnManager.Instance.currentPhase != TurnPhase.Move)
            return;

        TurnPlayerController currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogWarning("No active turn player found for tile movement.");
            return;
        }

        Ship ship = currentPlayer.GetComponent<Ship>();
        if (ship == null || ship.shipMovement == null)
        {
            Debug.LogWarning("Active turn player has no ship movement component.");
            return;
        }

        Vector3 tilePosition = transform.position;
        if (!ship.shipMovement.ReachableTileMoveCosts.TryGetValue(tilePosition, out int tileDistance))
        {
            Debug.Log("Tile is not currently reachable.");
            return;
        }

        ship.shipMovement.StartMove(tilePosition, tileDistance);
    }
}
