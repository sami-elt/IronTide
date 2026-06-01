using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private void Start()
    {
        ship = GetComponent<Ship>();

        //if (ship == null)
        //    return;
        //if (ship.shipInfo != null)
        //    ship.shipInfo.ResetValues();
    }

    private void Update()
    {
        
    }


    private void OnInteract()
    {
        if (enabled == false || !ship.turnPlayerController.IsMyTurn)
            return;

        switch (TurnManager.Instance.currentPhase)
        {

            case TurnPhase.Move:
                TryMoveShipToTileAtMouse();
                break;

            case TurnPhase.RollAttack:
            case TurnPhase.Attack:
                if (!ship.shipWeapon.HasAttacked)
                    TryAttackingShipAtMouse();
                break;
        }

    }


    private void TryMoveShipToTileAtMouse()
    {
        if (ship.shipMovement.Moving)
        {
            ship.shipMovement.SkipMove();
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!TryGetReachableTileFromRay(ray, out Vector3 tilePos, out int tileDistance))
        {
            Debug.Log("Tile is not currently reachable");
            return;
        }

        ship.shipMovement.StartMove(tilePos, tileDistance);
    }

    private void TryAttackingShipAtMouse()
    {
        //null check
        if (ship.shipWeapon == null) return; 

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!TryGetShipFromRay(ray, out Ship component))
        {
            Debug.Log("Tile does not hold player");
            return;
        }

        Vector3 hitPos = component.transform.position;
        if (!ship.shipWeapon.ReachableTargetsDamageModifiers.TryGetValue(hitPos, out int damageModifier))
        {
            Debug.Log("Player is not reachable");
            return;
        }

        ship.shipWeapon.SelectTarget(component.gameObject);
        ship.shipWeapon.Attack(damageModifier);
    }

    private bool TryGetReachableTileFromRay(Ray ray, out Vector3 tilePos, out int tileDistance)
    {
        tilePos = Vector3.zero;
        tileDistance = 0;

        RaycastHit[] hits = Physics.RaycastAll(ray);
        SortHitsByDistance(hits);

        for (int i = 0; i < hits.Length; i++)
        {
            HexTile tile = hits[i].collider.GetComponentInParent<HexTile>();
            Vector3 candidatePosition = tile != null ? tile.transform.position : hits[i].transform.position;
            if (!ship.shipMovement.ReachableTileMoveCosts.TryGetValue(candidatePosition, out int candidateDistance))
                continue;

            tilePos = candidatePosition;
            tileDistance = candidateDistance;
            return true;
        }

        return false;
    }

    private bool TryGetShipFromRay(Ray ray, out Ship targetShip)
    {
        targetShip = null;

        RaycastHit[] hits = Physics.RaycastAll(ray);
        SortHitsByDistance(hits);

        for (int i = 0; i < hits.Length; i++)
        {
            Ship candidate = hits[i].collider.GetComponentInParent<Ship>();
            if (candidate == null || candidate == ship)
            {
                HexTile tile = hits[i].collider.GetComponentInParent<HexTile>();
                Vector3 tilePosition = tile != null ? tile.transform.position : hits[i].transform.position;
                if (!TileOccupiedByPlayer(tilePosition, out candidate))
                    continue;
            }

            targetShip = candidate;
            return true;
        }

        return false;
    }

    private static void SortHitsByDistance(RaycastHit[] hits)
    {
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
    }


    private bool TileOccupiedByPlayer(Vector3 tilePos, out Ship targetShip)
    {
        targetShip = null;
        Ray ray = new(tilePos, Vector3.up);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        SortHitsByDistance(hits);

        for (int i = 0; i < hits.Length; i++)
        {
            Ship candidate = hits[i].collider.GetComponentInParent<Ship>();
            if (candidate == null || candidate == ship)
                continue;

            targetShip = candidate;
            return true;
        }

        return false;
    }
}
