using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ShipMovement : MonoBehaviour
{
    [SerializeField] private Ship ship;

    [SerializeField] private float speed = 20;
    private float moveIncrement;

    private float moveProgress;
    private Vector3 startPosition;
    private Vector3 endPosition;

    public bool Moving { get; private set; }

    public Dictionary<Vector3, int> ReachableTileMoveCosts { get; private set; } = new();
    public int avaliableTileDistance;

    public static float distanceBetweenTiles { get; } = 1.50f;//Since tiles are hexagonal they do not share the same distance in all directions but keeping value to the width works well enough on the current map size.

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

    public void EnterMovePhase(bool addBonus = true)
    {
        avaliableTileDistance = ship.shipInfo.GetMoveDistance(addBonus);
        TurnManager.BroadcastMovementRolled(avaliableTileDistance);
        FindReachableTiles();
        ship.shipWeapon.FindReachableTargets();
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
        if (Moving == false)
            return;

        Moving = false;
        moveProgress = 1;
        transform.position = endPosition;
        FindReachableTiles();
        ship.shipWeapon.FindReachableTargets();
    }

    private void Move()
    {
        moveProgress += moveIncrement * Time.deltaTime;
        transform.position = Vector3.Lerp(startPosition, endPosition, moveProgress);

        if (transform.position == endPosition)
        {
            Moving = false;
            FindReachableTiles();
            ship.shipWeapon.FindReachableTargets();
        }

    }

    //Goes through the straight paths the player can take and saves the position and cost for reachable tiles.
    private void FindReachableTiles()
    {
        ReachableTileMoveCosts.Clear();

        for (int side = 0; side < 6; side++)
        {
            Vector3 origin = transform.position;
            Vector3 direction = Quaternion.AngleAxis(30 +  side * 60, Vector3.up) * Vector3.forward;
            float tileSize = distanceBetweenTiles;

            //Debug.Log("side: " + side);

            for (int step = 0; step < ship.shipMovement.avaliableTileDistance; step++)
            {
                //Debug.Log("step: " + step);
                Vector3 stepPos = tileSize * direction + origin;
                Debug.DrawLine(origin, stepPos, Color.red, 5f);
                Debug.Log("FROM: " + origin + " TO: " + stepPos);
                stepPos.y += 10;

                if (!Physics.Raycast(stepPos, Vector3.down, out RaycastHit hitInfo))
                {
                    Debug.LogWarning("Broke because of missed raycast");
                    break;
                }
                Debug.Log(hitInfo.transform.name);
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
                    //Debug.LogWarning($"Broke because of obstacle or not walkable: shipComponent: {shipComponent}, tileComponent: {tileComponent}. Cast from position {stepPos}, hit object at {hitInfo.transform.position}.");
                    break;
                }
            }
            //Debug.LogWarning("Reached end of side " + side);
        }
    }


}
