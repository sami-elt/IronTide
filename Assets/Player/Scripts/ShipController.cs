using System.Runtime.InteropServices;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private readonly int playerLayer = 3;

    [SerializeField] private int phase;

    private void Start()
    {
        ship = GetComponent<Ship>();

        ship.shipInfo.ResetValues();
    }

    private void Update()
    {

    }

    private void OnInteract()
    {
        if (enabled == false)
            return;

        //switch (TurnManager.Instance.currentPhase)
        //{
        //    case TurnPhase.Move:
        //        TryMoveShipToTileAtMouse();
        //        break;

        //    case TurnPhase.Attack:
        //        TryAttackingShipAtMouse();
        //        break;
        //}

        switch (phase)
        {
            case 0:
                TryMoveShipToTileAtMouse();
                break;

            case 1:
                TryAttackingShipAtMouse();
                break;
        }
    }

    private void TryMoveShipToTileAtMouse()
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
            Debug.Log("Object hit by ray does not have the HexTile component, might be player in the way");
            return;
        }

        Vector3 tilePos = tile.gameObject.transform.position;
        if (!tile.isWalkable || TileOccupiedByPlayer(tilePos, out Ship s))
        {
            Debug.Log("Tile is not walkable or occupied");
            return;
        }
        tilePos.y = transform.position.y;

        if (!WithinAcceptableAngles(tilePos))
        {
            Debug.Log("No straight path to tile");
            return;
        }

        int tileDistance = GetTileDistanceToTarget(tilePos);
        if (ship.shipMovement.avaliableTileDistance < tileDistance)
        {
            Debug.Log("Target tile is too far for the current avaliable range");
            return;
        }

        if (ObsticlesOnPath(tilePos, tileDistance))
        {
            Debug.Log("Obsticle(s) in the way");
            return;
        }

        ship.shipMovement.StartMove(tilePos, tileDistance);
    }

    private void TryAttackingShipAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            Debug.Log("Ray did not hit any collider");
            return;
        }


        Vector3 targetPos = hitInfo.transform.position;
        if (targetPos == transform.position)
            return;

        targetPos.y = transform.position.y;

        if (!WithinAcceptableAngles(targetPos))
        {
            Debug.Log("No straight path to tile");
            return;
        }

        int tileDistance = GetTileDistanceToTarget(targetPos);
        if (tileDistance > ship.shipInfo.GetWeaponRange())
        {
            Debug.Log("Target out of range");
            return;
        }

        if (ObsticlesOnPath(targetPos, tileDistance))
        {
            Debug.Log("Obsticle(s) in the way");
            return;
        }

        if (hitInfo.collider.TryGetComponent(out Ship component))
        {
            component.shipInfo.Hurt(ship.shipInfo.GetWeaponDamage());
            Debug.Log("Hit first");
        }
        else
        {
            if (TileOccupiedByPlayer(hitInfo.transform.position, out component))
            {
                component.shipInfo.Hurt(ship.shipInfo.GetWeaponDamage());
                Debug.Log("Missed first but hit second");
            }
        }

        Debug.Log("Hit: " + component);
    }

    private bool WithinAcceptableAngles(Vector3 tilePos)
    {
        float angleToTarget = Vector3.Angle((tilePos - transform.position).normalized, Vector3.forward);

        //Rounding the angle to nearest 10th
        angleToTarget *= 0.1f;
        angleToTarget = (int)angleToTarget;
        angleToTarget *= 10;

        bool isAcceptedAngle = angleToTarget == 30 || angleToTarget == 90 || angleToTarget == 140;//Could likely be done better
        return isAcceptedAngle;
    }

    private int GetTileDistanceToTarget(Vector3 targetPos)
    {
        Vector3 tileDirection = (targetPos - transform.position).normalized;
        float tileSize = ship.shipMovement.distanceBetweenTiles;

        Vector3 stepPos = transform.position;
        Vector3 stepOrigin = stepPos;

        int tiles = 0;

        while(stepOrigin != targetPos && tiles < 20)
        {
            tiles++;

            stepPos = tileSize * tileDirection + stepOrigin;
            stepPos.y += 10;

            if (Physics.Raycast(stepPos, Vector3.down, out RaycastHit hitInfo))
            {
                Vector3 newOrigin = hitInfo.transform.position;
                stepOrigin.x = newOrigin.x;
                stepOrigin.z = newOrigin.z;
            }

        }

        return tiles;
    }

    private bool TileOccupiedByPlayer(Vector3 tilePos, out Ship ship)
    {
        ship = null;
        Ray ray = new(tilePos, Vector3.up);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, playerLayer))
        {
            if (hitInfo.collider.TryGetComponent(out Ship component))
            {
                ship = component;
            }

            return true;
        }
        return false;
    }

    private bool ObsticlesOnPath(Vector3 tilePos, int tileDistance)
    {
        Vector3 tileDirection = (tilePos - transform.position).normalized;
        float tileSize = ship.shipMovement.distanceBetweenTiles;
        Vector3 stepPos = transform.position;
        Vector3 stepOrigin = stepPos;

        for (int step = 0; step < tileDistance - 1; step++)
        {
            stepPos = tileSize * tileDirection + stepOrigin;
            stepPos.y += 10;

            if (Physics.Raycast(stepPos, Vector3.down, out RaycastHit hitInfo))
            {
                Vector3 newOrigin = hitInfo.transform.position;
                stepOrigin.x = newOrigin.x;
                stepOrigin.z = newOrigin.z;

                hitInfo.collider.TryGetComponent(out HexTile tileComponent);
                hitInfo.collider.TryGetComponent(out Ship shipComponent);

                if (shipComponent != null || !tileComponent.isWalkable)
                {
                    return true;
                }
            }
        }

        return false;
    }


    
}
