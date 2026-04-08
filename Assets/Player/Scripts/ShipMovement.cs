using System.ComponentModel;
using UnityEngine;

public class ShipMovement : MonoBehaviour
{
    [SerializeField] private Ship ship;

    [SerializeField] private float speed = 4;
    private float moveIncrement;

    private float moveProgress;
    private Vector3 startPosition;
    private Vector3 endPosition;

    private bool moving;

    public int avaliableTileDistance;

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


    public void StartMove(Vector3 targetPosition)
    {
        if (moving)
        {
            Debug.Log("Current move not finished, did not start new move");
            return;
        }

        startPosition = transform.position;
        endPosition = targetPosition;

        moveIncrement = speed / Vector3.Distance(endPosition, startPosition);

        moveProgress = 0;
        moving = true;
    }

    public void SkipMove()
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
