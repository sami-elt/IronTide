using UnityEngine;
using UnityEngine.InputSystem;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private readonly int playerLayer = 3;

    public GameObject activeIndicator;

    private float interactWait = 0.2f;
    private float nextInteract = 0f;


    private void Start()
    {
        ship = GetComponent<Ship>();

        ship.shipInfo.ResetValues();
    }

    private void Update()
    {
        if (Time.timeSinceLevelLoad < 3)
            return;

        if (activeIndicator.activeInHierarchy && !ship.turnPlayerController.IsMyTurn)
        {
            activeIndicator.SetActive(false);
        }
        else if (!activeIndicator.activeInHierarchy && ship.turnPlayerController.IsMyTurn)
        {
            activeIndicator.SetActive(true);
        }
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
        if (!ship.shipWeapon.ReachableTargetsDamageReduction.TryGetValue(hitPos, out int damageReduction))
        {
            Debug.Log("Player is not reachable");
            return;
        }

        ship.shipWeapon.SelectTarget(hitInfo.collider.gameObject);
        ship.shipWeapon.Attack(damageReduction);
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
