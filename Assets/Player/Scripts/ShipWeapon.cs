using UnityEngine;
using System.Collections.Generic;
using IronTide.BasicCards;
using TMPro;

public class ShipWeapon : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private ShipInfo target;
    private ShipInfo.WeaponDamageRoll preparedDamageRoll;
    private bool hasPreparedDamageRoll;

    public bool HasAttacked { get; private set; }

    public Dictionary<Vector3, int> ReachablePositionsDamageModifiers = new();
    public Dictionary<Vector3, int> ReachableTargetsDamageModifiers = new();
    public Dictionary<Vector3, int> ReachableTargetsDistance = new();
    public Dictionary<Vector3, int> ReachableTargetsCoverModifiers = new();

    [SerializeField] GameObject bulletObject;

    private void Awake()
    {
        ship = GetComponent<Ship>();
    }


    public void EnterAttackPhase()
    {
        HasAttacked = false;
        preparedDamageRoll = default;
        hasPreparedDamageRoll = false;
        FindReachableTargets();

        if (ReachableTargetsDamageModifiers.Count > 0)
        {
            preparedDamageRoll = ship.shipInfo.RollWeaponDamage(null);
            hasPreparedDamageRoll = true;
            TurnManager.BroadcastAttackPrepared(preparedDamageRoll.DiceTotal, preparedDamageRoll.BonusTotal);
        }
    }

    public void Attack(int damageModifier)
    {
        if (target != null)
        {
            //skapa skottet
            if (bulletObject != null)
            {
                // Skapar prefab p� skeppets position
                GameObject newBullet = Instantiate(bulletObject, transform.position, Quaternion.identity);

            
                WeaponProjectiles projScript = newBullet.GetComponent<WeaponProjectiles>();

                // Kollar om det finns script
                if (projScript != null)
                {
                    projScript.Shoot(target.transform);
                }
            }

          
            
            Vector3 targetPosition = target.transform.position;
            ReachableTargetsDistance.TryGetValue(targetPosition, out int distance);
            ReachableTargetsCoverModifiers.TryGetValue(targetPosition, out int coverModifier);

            bool targetWasSunk = target.Sunk;
            int rangeModifier = damageModifier - coverModifier;
            ShipInfo.WeaponDamageRoll damageRoll = hasPreparedDamageRoll
                ? preparedDamageRoll
                : ship.shipInfo.RollWeaponDamage(target);
            int totalBonus = damageRoll.BonusTotal + damageModifier;
            int rawDamage = Mathf.Max(0, damageRoll.DiceTotal + totalBonus);
            int damageReduction = target.GetDamageReduction(rangeModifier, coverModifier);
            int dealtDamage = target.Hurt(rawDamage, rangeModifier, coverModifier);
            if (!targetWasSunk && target.Sunk && ship.turnPlayerController != null)
                IronTideGameState.RecordFirstKill(ship.turnPlayerController.playerID);

            TurnManager.BroadcastAttackRolled(rawDamage);
            TurnManager.BroadcastDamageDealt(dealtDamage);
            TurnManager.BroadcastAttackResolved(damageRoll.DiceTotal, totalBonus, damageReduction, dealtDamage);
            ShowDamagePopup(target.transform.position, dealtDamage);
            hasPreparedDamageRoll = false;
            target = null;
            HasAttacked = true;
        }

    }

    private void ShowDamagePopup(Vector3 targetPosition, int dealtDamage)
    {
        GameObject popupObject = new GameObject("Damage Popup");
        popupObject.transform.position = targetPosition + Vector3.up * 1.8f;

        TextMeshPro text = popupObject.AddComponent<TextMeshPro>();
        text.text = $"-{dealtDamage}";
        text.fontSize = 4f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.18f, 0.12f, 1f);
        text.fontStyle = FontStyles.Bold;

        if (Camera.main != null)
            popupObject.transform.rotation = Quaternion.LookRotation(popupObject.transform.position - Camera.main.transform.position);

        Destroy(popupObject, 1.4f);
    }

    public void SelectTarget(GameObject targetObject)
    {
        if (targetObject.TryGetComponent<ShipInfo>(out var newTarget))
        {
            target = newTarget;
        }
    }

    //Goes through the straight paths the player can take and saves the position and damage modifiers of possible targets.
    public void FindReachableTargets()
    {
        ReachablePositionsDamageModifiers.Clear();
        ReachableTargetsDamageModifiers.Clear();
        ReachableTargetsDistance.Clear();
        ReachableTargetsCoverModifiers.Clear();

        for (int side = 0; side < 6; side++)
        {
            Vector3 origin = transform.position;
            Vector3 direction = Quaternion.AngleAxis(30 + side * 60, Vector3.up) * Vector3.forward;
            float tileSize = ShipMovement.DistanceBetweenTiles;
            int obstacleDamageModifier = 0;

            //Debug.Log("side: " + side);

            for (int step = 0; step < ship.shipInfo.GetWeaponRange(); step++)
            {
                //Debug.Log("step: " + step);
                Vector3 stepPos = tileSize * direction + origin;
                stepPos.y += 10;


                if (!Physics.Raycast(stepPos, Vector3.down, out RaycastHit hitInfo))
                {
                    //Debug.LogWarning("Broke because of missed raycast");
                    break;
                }

                Vector3 newOrigin = hitInfo.transform.position;
                origin.x = newOrigin.x;
                origin.z = newOrigin.z;

                hitInfo.collider.TryGetComponent(out HexTile tileComponent);
                hitInfo.collider.TryGetComponent(out Ship shipComponent);

                bool usingLongRange = ship.shipInfo.WeaponModule != null && ship.shipInfo.WeaponModule.Archetype == IronTideModuleArchetype.LongRangeWeapon;
                bool ignoresRocks = ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "mortar");
                bool tileIsWalkable = tileComponent != null && tileComponent.isWalkable;

                int distanceDamageModifier = ship.shipInfo.GetDistanceDamageModifier(step + 1);

                if (shipComponent != null)
                {    
                    ReachablePositionsDamageModifiers.TryAdd(shipComponent.transform.position, obstacleDamageModifier + distanceDamageModifier);
                    ReachableTargetsDamageModifiers.TryAdd(shipComponent.transform.position, obstacleDamageModifier + distanceDamageModifier);
                    ReachableTargetsDistance.TryAdd(shipComponent.transform.position, step + 1);
                    ReachableTargetsCoverModifiers.TryAdd(shipComponent.transform.position, obstacleDamageModifier);
                }
                else if(tileIsWalkable)
                {
                    ReachablePositionsDamageModifiers.TryAdd(tileComponent.transform.position, obstacleDamageModifier + distanceDamageModifier);
                }
                else if (usingLongRange && !tileIsWalkable)
                {
                    if (!ignoresRocks)
                        obstacleDamageModifier -= 2;
                }
                else if (!tileIsWalkable)
                {
                    //Debug.LogWarning("Broke because of hitting a blocking tile");
                    break;
                }

            }
            //Debug.LogWarning("Reached end of side " + side);
        }
    }
}
