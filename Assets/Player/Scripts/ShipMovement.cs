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

    private bool moving;

    public int avaliableTileDistance;

    public float distanceBetweenTiles;//Since tiles are hexagonal they do not share the same distance in all directions but keeping value to the width works well enough on the current map size.

    private void Start()
    {
        ship = GetComponent<Ship>();

        moving = false;
        moveIncrement = 0;

    }

    private void Update()
    {
        if (moving)
            Move();
    }

    public void EnterMovePhase(bool addBonus = true)
    {
        avaliableTileDistance = ship.shipInfo.GetMoveDistance(addBonus);

    }


    public void StartMove(Vector3 targetPosition, int tilesMoved)
    {
        if (moving)
        {
            Debug.Log("Current move not finished, did not start new move");
            SkipMove();
            return;
        }

        targetPosition.y = transform.position.y;

        startPosition = transform.position;
        endPosition = targetPosition;

        moveIncrement = speed / Vector3.Distance(endPosition, startPosition);

        moveProgress = 0;
        moving = true;
        avaliableTileDistance -= tilesMoved;
    }

    private void SkipMove()
    {
        if (moving == false)
            return;

        moving = false;
        moveProgress = 1;
        transform.position = endPosition;
    }

    private void Move()
    {
        moveProgress += moveIncrement * Time.deltaTime;
        transform.position = Vector3.Lerp(startPosition, endPosition, moveProgress);

        if (transform.position == endPosition)
        {
            moving = false;
        }

    }
}
