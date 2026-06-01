using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShipReachVisuals : MonoBehaviour
{
    [SerializeField] private Ship ship;

    [SerializeField] private GameObject reachableVisual;
    [SerializeField] private GameObject enemyVisual;

    private List<GameObject> visuals = new();
    private List<Vector3> positions = new();
    private bool visualsDrawn;

    private List<GameObject> extraVisuals = new();
    private List<Vector3> extraPositions = new();
    private bool extraVisualsDrawn;


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
                visualsDrawn = false;
            }
            ClearVisuals();
        }



        if (phase == TurnPhase.Move)
        {
            UpdateMovePhaseVisual();
        }


        if (phase == TurnPhase.Attack)
        {
            UpdateAttackPhaseVisual();
        }


    }

    private void UpdateMovePhaseVisual()
    {
        bool moving = ship.shipMovement.Moving;
        int currentTileDistance = ship.shipMovement.avaliableTileDistance;
        if (currentTileDistance > 0 && !moving && !visualsDrawn)
        {
            ShowWalkable();
            ShowHittable(false);
            visualsDrawn = true;
        }
        else if (moving && visualsDrawn)
        {
            visualsDrawn = false;
            ClearVisuals();
        }
    }

    private void UpdateAttackPhaseVisual()
    {
        bool hasAttacked = ship.shipWeapon.HasAttacked;
        if (!hasAttacked && !visualsDrawn)
        {
            ShowHittable();
        }
        else if (hasAttacked && visualsDrawn)
        {
            visualsDrawn = false;
            ClearVisuals();
        }
    }

    public void UpdateExtraVisual()
    {
        //Add logic for displaying and clearing extra visuals, such as showing reachable tiles while a certain key is held
    }

    private void ShowWalkable(bool clearVisuals = true)
    {
        if (clearVisuals)
            ClearVisuals();

        Dictionary<Vector3, int> moveCosts = ship.shipMovement.ReachableTileMoveCosts;
        positions = new(moveCosts.Keys);

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject visual = Instantiate(reachableVisual, positions[i], Quaternion.identity);
            TMP_Text visualText = visual.GetComponentInChildren<TMP_Text>();
            if (visualText != null)
                visualText.text = moveCosts[positions[i]].ToString();
            visuals.Add(visual);
            visualsDrawn = true;
        }

    }

    private void ShowReachable(bool clearVisuals = true)
    {
        if (clearVisuals)
            ClearVisuals();

        Dictionary<Vector3, int> damageModifiers = ship.shipWeapon.ReachablePositionsDamageModifiers;
        positions = new(damageModifiers.Keys);

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject visual = Instantiate(enemyVisual, positions[i], Quaternion.identity);

            TMP_Text visualText = visual.GetComponentInChildren<TMP_Text>();
            int damageModifier = damageModifiers[positions[i]];
            if (visualText != null)
                visualText.text = damageModifier == 0 ? "" : $"{damageModifier}";

            visuals.Add(visual);
            visualsDrawn = true;
        }

    }

    private void ShowHittable(bool clearVisuals = true)
    {
        if (clearVisuals)
            ClearVisuals();

        Dictionary<Vector3, int> damageModifiers = ship.shipWeapon.ReachableTargetsDamageModifiers;
        positions = new(damageModifiers.Keys);

        for (int i = 0; i < positions.Count; i++)
        {
            GameObject visual = Instantiate(enemyVisual, positions[i], Quaternion.identity);

            TMP_Text visualText = visual.GetComponentInChildren<TMP_Text>();
            if (visualText != null &&
                ship.shipWeapon.TryGetPredictedDamageRange(positions[i], out int lowBound, out int highBound))
                visualText.text = $"{lowBound}-{highBound}";

            visuals.Add(visual);
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

    private void ClearExtraVisuals()
    {
        if (extraVisuals.Count == 0)
            return;

        foreach (GameObject g in extraVisuals)
        {
            Destroy(g);
        }
        extraVisuals.Clear();
    }
}
