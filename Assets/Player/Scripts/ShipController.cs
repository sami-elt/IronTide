using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Ship ship;




    private void Start()
    {
        ship = GetComponent<Ship>();

    }

    private void Update()
    {

    }

    private void OnInteract()
    {
        //Should only be done if player wishes to move/has the ability to move
        TryMoveShipToTileAtMouse();
    }

    public void TryMoveShipToTileAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Debug.Log("Ray did not hit any collider");
            return;
        }

        HexTile tile;
        if (hitInfo.collider.TryGetComponent(out HexTile component))
        {
            tile = component;
        }
        else
        {
            Debug.Log("Object hit by ray does not have the HexTile component");
            return;
        }

        if (!tile.isWalkable)
        {
            Debug.Log("Tile is not walkable");
            return;
        }

        Vector3 tilePos = tile.gameObject.transform.position;
        tilePos.y = transform.position.y;
        int tileDistance =  (int)(Vector3.Distance(tilePos, transform.position) / ship.shipMovement.distanceBetweenTiles);
        if (ship.shipMovement.avaliableTileDistance < tileDistance)
        {
            Debug.Log("Target tile is too far for the current avaliable range");
            return;
        }

        float angleToTarget = Vector3.Angle((tilePos - transform.position).normalized, Vector3.forward);

        //Rounding the angle to nearest 10th
        angleToTarget *= 0.1f;
        angleToTarget = (int)angleToTarget;
        angleToTarget *= 10;
        
        bool acceptedAngle = angleToTarget == 30 || angleToTarget == 90 || angleToTarget == 140;//Could likely be done better
        if (!acceptedAngle)
        {
            Debug.Log("No straight path to tile");
            return;
        }

        if (ObsticlesOnPath(tilePos, tileDistance))
        {
            Debug.Log("Obsticle(s) in the way");
            return;
        }

        ship.shipMovement.StartMove(tilePos,tileDistance);
    }

    private bool ObsticlesOnPath(Vector3 tilePos, int tileDistance)
    {
        Vector3 tileDirection = (tilePos - transform.position).normalized;
        float tileSize = ship.shipMovement.distanceBetweenTiles;

        for (int step = 0; step < tileDistance; step++)
        {
            Vector3 stepPos = (1 + step) * tileSize * tileDirection + transform.position;
            stepPos.y += 10;

            if (Physics.Raycast(stepPos, Vector3.down, out RaycastHit hitInfo))
            {
                if (!hitInfo.collider.GetComponent<HexTile>().isWalkable)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void VisualizeReachableTiles()
    {

    }

    public void VisualizeWeaponRange()
    {

    }
}
