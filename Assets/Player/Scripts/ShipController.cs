using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private readonly int playerLayer = 3;

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

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            return;
        }


        Vector3 tilePos = hitInfo.transform.position;
        if (!ship.shipMovement.ReachableTileMoveCosts.TryGetValue(tilePos, out int tileDistance))
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

        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            return;
        }

        Vector3 hitPos = hitInfo.transform.position;
        if (!hitInfo.collider.TryGetComponent(out Ship component))
        {
            if (!TileOccupiedByPlayer(hitPos, out component))
            {
                Debug.Log("Tile does not hold player");
                return;
            }
        }

        hitPos = component.transform.position;
        if (!ship.shipWeapon.ReachableTargetsDamageModifiers.TryGetValue(hitPos, out int damageModifier))
        {
            Debug.Log("Player is not reachable");
            return;
        }

        ship.shipWeapon.SelectTarget(hitInfo.collider.gameObject);
        ship.shipWeapon.Attack(damageModifier);
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
}
