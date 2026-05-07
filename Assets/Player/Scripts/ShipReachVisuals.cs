using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;

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
            Dictionary<Vector3, int> moveCosts = ship.shipMovement.ReachableTileMoveCosts;
            positions = new(moveCosts.Keys);
            ClearVisuals();

            for (int i = 0; i < positions.Count; i++)
            {
                GameObject visual = Instantiate(reachableVisual, positions[i], Quaternion.identity);
                TMP_Text visualText = visual.GetComponentInChildren<TMP_Text>();
                visualText.text = moveCosts[positions[i]].ToString();
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
            Dictionary<Vector3, int> damageReductions = ship.shipWeapon.ReachablePositionsDamageModifiers;
            positions = new(damageReductions.Keys);
            ClearVisuals();

            for (int i = 0; i < positions.Count; i++)
            {
                GameObject visual = Instantiate(enemyVisual, positions[i], Quaternion.identity);

                TMP_Text visualText = visual.GetComponentInChildren<TMP_Text>();
                int damageReduction = damageReductions[positions[i]];
                if (damageReduction == 0)
                    visualText.text = "+-0";
                else
                    visualText.text = $"-{damageReduction}";

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
