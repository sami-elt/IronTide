using System.Collections.Generic;
using UnityEngine;

public class ShipMovement : MonoBehaviour
{
    [SerializeField] public Ship ship;

    [SerializeField] private float speed = 20;
    private float moveIncrement;

    private float moveProgress;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool moveStartedAsFirstMove;

    public bool Moving { get; private set; }

    public Dictionary<Vector3, int> ReachableTileMoveCosts { get; private set; } = new();
    public int avaliableTileDistance;

    public static float DistanceBetweenTiles { get; } = 1.50f;

    private void Awake()
    {
        ship = GetComponent<Ship>();
    }

    private void Start()
    {
        Moving = false;
        moveIncrement = 0;
    }

    private void Update()
    {
        if (Moving)
            Move();
    }

    public bool isWaitingForDice = false;

    public void EnterMovePhase(bool addBonus = true)
    {

        isWaitingForDice = true;


        //avaliableTileDistance = ship.shipInfo.GetMoveDistance(addBonus);

        int sides = ship.shipInfo.GetEngineDice();

        FindFirstObjectByType<DiceManager>().ActiveDice(sides);

        //TurnManager.BroadcastMovementRolled(avaliableTileDistance);
        //FindReachableTiles();
        //ship.shipWeapon.FindReachableTargets();
    }

    public void ReceiveDiceResult(int result)
    {
        isWaitingForDice = false;
        //FindFirstObjectByType<DiceManager>().HideAllDice();


        avaliableTileDistance = ship.shipInfo.CalculateMoveDistance(result, true);

        TurnManager.BroadcastMovementRolled(avaliableTileDistance);
        FindReachableTiles();
        ship.shipWeapon.FindReachableTargets();

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.StartMovePhase();
        }
    }


    public void StartMove(Vector3 targetPosition, int tilesMoved)
    {
        if (Moving)
        {
            Debug.Log("Current move not finished, did not start new move");
            return;
        }

        ReachableTileMoveCosts.Clear();
        Moving = true;
        moveProgress = 0;
        moveStartedAsFirstMove = TurnManager.Instance == null || TurnManager.Instance.MovesUsedthisTurn == 0;

        targetPosition.y = transform.position.y;

        startPosition = transform.position;
        endPosition = targetPosition;

        moveIncrement = speed / Vector3.Distance(endPosition, startPosition);
        avaliableTileDistance -= tilesMoved;

        Quaternion newRotation = Quaternion.FromToRotation(Vector3.forward, (endPosition - startPosition).normalized);
        transform.rotation = newRotation;
    }

    public void SkipMove()
    {
        if (!Moving)
            return;

        CompleteMove();
    }

    private void Move()
    {
        moveProgress += moveIncrement * Time.deltaTime;
        transform.position = Vector3.Lerp(startPosition, endPosition, moveProgress);

        if (moveProgress >= 1f)
            CompleteMove();
    }

    private void CompleteMove()
    {
        Moving = false;
        moveProgress = 1f;
        transform.position = endPosition;
        ResolveMoveEndPassives();
        FindReachableTiles();
        ship.shipWeapon.FindReachableTargets();
    }

    private void ResolveMoveEndPassives()
    {
        if (!moveStartedAsFirstMove || !ship.shipInfo.HasActivePassive(ship.shipInfo.EngineModule, "ramming_speed_t2"))
            return;

        ShipInfo[] ships = FindObjectsByType<ShipInfo>(FindObjectsSortMode.None);
        for (int i = 0; i < ships.Length; i++)
        {
            ShipInfo target = ships[i];
            if (target == null || target == ship.shipInfo || target.Sunk || !IsAdjacent(target.transform.position))
                continue;

            bool targetWasSunk = target.Sunk;
            ShipInfo.DamageResult result = target.LoseHealthDirect(1, ship.shipInfo);
            if (result.ModuleDestroyed && ship.shipInfo.HasActivePassive(ship.shipInfo.ArmorModule, "scrapper_t2"))
                ship.shipInfo.Heal(3);

            if (!targetWasSunk && target.Sunk && ship.turnPlayerController != null)
                IronTideGameState.RecordFirstKill(ship.turnPlayerController.playerID);
        }
    }

    private bool IsAdjacent(Vector3 position)
    {
        Vector3 delta = position - transform.position;
        delta.y = 0f;
        return delta.magnitude <= DistanceBetweenTiles * 1.45f;
    }

    private void FindReachableTiles()
    {
        ReachableTileMoveCosts.Clear();

        for (int side = 0; side < 6; side++)
        {
            Vector3 direction = Quaternion.AngleAxis(30 + side * 60, Vector3.up) * Vector3.forward;
            FindReachableTilesInDirection(direction, DistanceBetweenTiles);
        }

        if (!ship.shipInfo.HasActivePassive(ship.shipInfo.EngineModule, "queen_of_the_sea_legendary"))
            return;

        for (int side = 0; side < 6; side++)
        {
            Vector3 direction = Quaternion.AngleAxis(side * 60, Vector3.up) * Vector3.forward;
            FindReachableTilesInDirection(direction, DistanceBetweenTiles * Mathf.Sqrt(3f));
        }
    }

    private void FindReachableTilesInDirection(Vector3 direction, float tileSize)
    {
        Vector3 origin = transform.position;

        for (int step = 0; step < ship.shipMovement.avaliableTileDistance; step++)
        {
            Vector3 stepPos = tileSize * direction + origin;
            stepPos.y += 10;

            if (!Physics.Raycast(stepPos, Vector3.down, out RaycastHit hitInfo))
                break;

            Vector3 newOrigin = hitInfo.transform.position;
            origin.x = newOrigin.x;
            origin.z = newOrigin.z;

            hitInfo.collider.TryGetComponent(out HexTile tileComponent);
            hitInfo.collider.TryGetComponent(out Ship shipComponent);

            if (shipComponent == null && tileComponent != null && tileComponent.isWalkable)
            {
                ReachableTileMoveCosts.TryAdd(hitInfo.transform.position, step + 1);
            }
            else
            {
                break;
            }
        }
    }
}
