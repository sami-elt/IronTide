using System.Collections.Generic;
using UnityEngine;

public class ShipReachVisuals : MonoBehaviour
{
    [SerializeField] private Ship ship;

    [SerializeField] private GameObject reachableVisual;
    [SerializeField] private GameObject enemyVisual;

    private List<GameObject> visuals = new();
    private List<Vector3> positions = new();

    private bool visualsDrawn;

    private int previousTileDistance;

    private void Start()
    {
        ship = GetComponent<Ship>();
    }

    private void Update()
    {
        if (!ship.turnPlayerController.IsMyTurn)
        {
            ClearVisuals();
            return;
        }

        TurnPhase phase = TurnManager.Instance.currentPhase;
        
        if (phase == TurnPhase.RollMovement || phase == TurnPhase.RollAttack)
        {
            if (visualsDrawn == true)
            {
                visualsDrawn = false;
            }
            ClearVisuals();
        }



        if (phase == TurnPhase.Move)
            ShowWalkable();

        if (phase == TurnPhase.Attack)
            ShowHitable();

    }

    private void ShowWalkable()
    {
        int currentTileDistance = ship.shipMovement.avaliableTileDistance;
        if (currentTileDistance > 0 && !visualsDrawn)
        {
            previousTileDistance = currentTileDistance;
            positions = new(ship.shipMovement.ReachableTileMoveCosts.Keys);
            ClearVisuals();

            for (int i = 0; i < positions.Count; i++)
            {
                GameObject visual = Instantiate(reachableVisual, positions[i], Quaternion.identity);
                visuals.Add(visual);

                visualsDrawn = true;
            }
        }
        else if (currentTileDistance != previousTileDistance && visualsDrawn)
        {
            visualsDrawn = false;
        }
    }

    private void ShowHitable()
    {
        bool hasAttacked = ship.shipWeapon.HasAttacked;
        if (!hasAttacked && !visualsDrawn)
        {
            positions = new(ship.shipWeapon.ReachableTargetsDamageReduction.Keys);
            ClearVisuals();

            for (int i = 0; i < positions.Count; i++)
            {
                GameObject visual = Instantiate(enemyVisual, positions[i], Quaternion.identity);
                visuals.Add(visual);
            }
            visualsDrawn = true;
        }
    }

    private void ClearVisuals()
    {
        if (visuals.Count == 0)
            return;

        foreach (GameObject g in visuals)
        {
            Destroy(g);
        }
        visuals.Clear();
    }
}
