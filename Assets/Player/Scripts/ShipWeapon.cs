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
    public Dictionary<Vector3, IronTideAttackLineType> ReachableTargetsLineTypes = new();
    public Dictionary<Vector3, int> ReachableTargetsLineSides = new();

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
        if (target == null)
            return;

        FireProjectile(target);

        ShipInfo primaryTarget = target;
        Vector3 selectedTargetPosition = primaryTarget.transform.position;
        ReachableTargetsDistance.TryGetValue(selectedTargetPosition, out int distance);
        ReachableTargetsCoverModifiers.TryGetValue(selectedTargetPosition, out int coverModifier);
        ReachableTargetsLineTypes.TryGetValue(selectedTargetPosition, out IronTideAttackLineType lineType);

        bool targetWasSunk = primaryTarget.Sunk;
        ResolvePreAttackPassives(primaryTarget);
        ApplyBoardingPassives(primaryTarget);

        ShipInfo.WeaponDamageRoll damageRoll = hasPreparedDamageRoll
            ? preparedDamageRoll
            : ship.shipInfo.RollWeaponDamage(null);

        ShipInfo.DamageResult result = ResolveDamage(primaryTarget, damageRoll, damageModifier, coverModifier, lineType);
        ApplyPostDamagePassives(primaryTarget, result, targetWasSunk);

        ResolvePiercingShot(primaryTarget, selectedTargetPosition, damageRoll);

        if (TurnManager.Instance != null)
            TurnManager.Instance.RecordShipAttack(ship.shipInfo, primaryTarget);

        hasPreparedDamageRoll = false;
        target = null;
        HasAttacked = true;
    }

    private ShipInfo.DamageResult ResolveDamage(ShipInfo damageTarget, ShipInfo.WeaponDamageRoll damageRoll,
        int damageModifier, int coverModifier, IronTideAttackLineType lineType)
    {
        int rangeModifier = damageModifier - coverModifier;
        int contextModifier = ship.shipInfo.GetAttackContextDamageModifier(damageTarget, lineType);
        int totalBonus = damageRoll.BonusTotal + damageModifier + contextModifier;
        int rawDamage = Mathf.Max(0, damageRoll.DiceTotal + totalBonus);

        if (damageTarget.ShouldCancelCriticalAttack(damageRoll))
        {
            var canceledResult = new ShipInfo.DamageResult
            {
                RawDamage = rawDamage,
                WasCancelled = true
            };

            TurnManager.BroadcastAttackRolled(rawDamage);
            TurnManager.BroadcastDamageDealt(0);
            TurnManager.BroadcastAttackResolved(damageRoll.DiceTotal, totalBonus, 0, 0);
            ShowFloatingText(damageTarget.transform.position, "MISS", new Color(0.8f, 0.9f, 1f, 1f));
            return canceledResult;
        }

        bool allowOverkill = ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "overkill_legendary");
        ShipInfo.DamageResult result = damageTarget.HurtDetailed(rawDamage, ship.shipInfo, rangeModifier, coverModifier,
            lineType, allowOverkill);

        TurnManager.BroadcastAttackRolled(rawDamage);
        TurnManager.BroadcastDamageDealt(result.DamageDealt);
        TurnManager.BroadcastAttackResolved(damageRoll.DiceTotal, totalBonus, result.DamageReduction, result.DamageDealt);
        ShowDamagePopup(damageTarget.transform.position, result.DamageDealt);
        return result;
    }

    private void ApplyPostDamagePassives(ShipInfo damagedTarget, ShipInfo.DamageResult result, bool targetWasSunk)
    {
        if (!targetWasSunk && damagedTarget.Sunk && ship.turnPlayerController != null)
            IronTideGameState.RecordFirstKill(ship.turnPlayerController.playerID);

        if (result.ModuleDestroyed && ship.shipInfo.HasActivePassive(ship.shipInfo.ArmorModule, "scrapper_t2"))
            ship.shipInfo.Heal(3);
    }

    private void ResolvePreAttackPassives(ShipInfo attackTarget)
    {
        int pullDistance = 0;
        if (ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "grappling_cannon_t1"))
            pullDistance = 1;
        else if (ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "grappling_cannon_ii_t2"))
            pullDistance = 2;

        for (int i = 0; i < pullDistance; i++)
        {
            if (!TryPullTargetOneTile(attackTarget))
                break;
        }
    }

    private void ApplyBoardingPassives(ShipInfo attackTarget)
    {
        if (!AreShipsAdjacent(ship.shipInfo, attackTarget))
            return;

        if (ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "boarding_party_t1"))
            IronTideGameState.TryTransferGold(GetPlayerId(attackTarget), GetPlayerId(ship.shipInfo), 1);

        if (ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "boarding_party_ii_t2"))
            ship.shipInfo.Heal(2);
    }

    private void ResolvePiercingShot(ShipInfo primaryTarget, Vector3 primaryTargetPosition,
        ShipInfo.WeaponDamageRoll damageRoll)
    {
        if (!ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "piercing_shot_t2"))
            return;

        if (!ReachableTargetsLineSides.TryGetValue(primaryTargetPosition, out int primarySide))
            return;

        ReachableTargetsDistance.TryGetValue(primaryTargetPosition, out int primaryDistance);
        ShipInfo piercingTarget = FindPiercingTarget(primaryTarget, primarySide, primaryDistance);
        if (piercingTarget == null)
            return;

        Vector3 piercingPosition = piercingTarget.transform.position;
        if (!ReachableTargetsDamageModifiers.TryGetValue(piercingPosition, out int piercingDamageModifier))
            return;

        ReachableTargetsCoverModifiers.TryGetValue(piercingPosition, out int coverModifier);
        ReachableTargetsLineTypes.TryGetValue(piercingPosition, out IronTideAttackLineType lineType);

        bool targetWasSunk = piercingTarget.Sunk;
        ShipInfo.DamageResult result = ResolveDamage(piercingTarget, damageRoll, piercingDamageModifier, coverModifier, lineType);
        ApplyPostDamagePassives(piercingTarget, result, targetWasSunk);

        if (TurnManager.Instance != null)
            TurnManager.Instance.RecordShipAttack(ship.shipInfo, piercingTarget);
    }

    private ShipInfo FindPiercingTarget(ShipInfo primaryTarget, int primarySide, int primaryDistance)
    {
        ShipInfo fallbackTarget = null;
        int fallbackDistance = int.MaxValue;
        ShipInfo fartherTarget = null;
        int fartherDistance = int.MaxValue;

        foreach (var pair in ReachableTargetsLineSides)
        {
            if (pair.Value != primarySide)
                continue;

            ShipInfo candidate = FindShipAtPosition(pair.Key);
            if (candidate == null || candidate == primaryTarget || candidate.Sunk)
                continue;

            ReachableTargetsDistance.TryGetValue(pair.Key, out int candidateDistance);
            if (candidateDistance > primaryDistance && candidateDistance < fartherDistance)
            {
                fartherDistance = candidateDistance;
                fartherTarget = candidate;
            }

            if (candidateDistance < fallbackDistance)
            {
                fallbackDistance = candidateDistance;
                fallbackTarget = candidate;
            }
        }

        return fartherTarget != null ? fartherTarget : fallbackTarget;
    }

    private ShipInfo FindShipAtPosition(Vector3 position)
    {
        ShipInfo[] ships = FindObjectsByType<ShipInfo>(FindObjectsSortMode.None);
        for (int i = 0; i < ships.Length; i++)
        {
            ShipInfo candidate = ships[i];
            if (candidate == null || candidate == ship.shipInfo)
                continue;

            Vector3 delta = candidate.transform.position - position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.25f)
                return candidate;
        }

        return null;
    }

    private bool TryPullTargetOneTile(ShipInfo attackTarget)
    {
        Vector3 direction = transform.position - attackTarget.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return false;

        Vector3 stepPosition = attackTarget.transform.position + direction.normalized * ShipMovement.DistanceBetweenTiles;
        stepPosition.y += 10f;

        if (!Physics.Raycast(stepPosition, Vector3.down, out RaycastHit hitInfo))
            return false;

        if (hitInfo.collider.TryGetComponent(out Ship blockingShip) && blockingShip.shipInfo != attackTarget)
            return false;

        if (!hitInfo.collider.TryGetComponent(out HexTile tileComponent) || !tileComponent.isWalkable)
            return false;

        Vector3 destination = hitInfo.transform.position;
        if (IsOccupiedByOtherShip(destination, attackTarget))
            return false;

        destination.y = attackTarget.transform.position.y;
        attackTarget.transform.position = destination;
        return true;
    }

    private bool IsOccupiedByOtherShip(Vector3 position, ShipInfo movingShip)
    {
        ShipInfo[] ships = FindObjectsByType<ShipInfo>(FindObjectsSortMode.None);
        for (int i = 0; i < ships.Length; i++)
        {
            ShipInfo candidate = ships[i];
            if (candidate == null || candidate == movingShip || candidate.Sunk)
                continue;

            Vector3 delta = candidate.transform.position - position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.25f)
                return true;
        }

        return false;
    }

    private void FireProjectile(ShipInfo attackTarget)
    {
        if (bulletObject == null)
            return;

        GameObject newBullet = Instantiate(bulletObject, transform.position, Quaternion.identity);
        WeaponProjectiles projScript = newBullet.GetComponent<WeaponProjectiles>();
        if (projScript != null)
            projScript.Shoot(attackTarget.transform);
    }

    private void ShowDamagePopup(Vector3 targetPosition, int dealtDamage)
    {
        ShowFloatingText(targetPosition, $"-{dealtDamage}", new Color(1f, 0.18f, 0.12f, 1f));
    }

    private void ShowFloatingText(Vector3 targetPosition, string message, Color color)
    {
        GameObject popupObject = new GameObject("Damage Popup");
        popupObject.transform.position = targetPosition + Vector3.up * 1.8f;

        TextMeshPro text = popupObject.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = 4f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.fontStyle = FontStyles.Bold;

        if (Camera.main != null)
            popupObject.transform.rotation = Quaternion.LookRotation(popupObject.transform.position - Camera.main.transform.position);

        Destroy(popupObject, 1.4f);
    }

    public void SelectTarget(GameObject targetObject)
    {
        if (targetObject == null)
            return;

        if (targetObject.TryGetComponent<ShipInfo>(out var newTarget))
        {
            target = newTarget;
            return;
        }

        if (targetObject.TryGetComponent<Ship>(out var selectedShip) && selectedShip.shipInfo != null)
            target = selectedShip.shipInfo;
    }

    public void FindReachableTargets()
    {
        ReachablePositionsDamageModifiers.Clear();
        ReachableTargetsDamageModifiers.Clear();
        ReachableTargetsDistance.Clear();
        ReachableTargetsCoverModifiers.Clear();
        ReachableTargetsLineTypes.Clear();
        ReachableTargetsLineSides.Clear();

        for (int side = 0; side < 6; side++)
        {
            Vector3 origin = transform.position;
            Vector3 direction = Quaternion.AngleAxis(30 + side * 60, Vector3.up) * Vector3.forward;
            float tileSize = ShipMovement.DistanceBetweenTiles;
            int obstacleDamageModifier = 0;
            IronTideAttackLineType lineType = GetAttackLineType(side);

            for (int step = 0; step < ship.shipInfo.GetWeaponRange(); step++)
            {
                Vector3 stepPos = tileSize * direction + origin;
                stepPos.y += 10f;

                if (!Physics.Raycast(stepPos, Vector3.down, out RaycastHit hitInfo))
                    break;

                Vector3 newOrigin = hitInfo.transform.position;
                origin.x = newOrigin.x;
                origin.z = newOrigin.z;

                hitInfo.collider.TryGetComponent(out HexTile tileComponent);
                hitInfo.collider.TryGetComponent(out Ship shipComponent);

                bool usingLongRange = ship.shipInfo.WeaponModule != null &&
                    ship.shipInfo.WeaponModule.Archetype == IronTideModuleArchetype.LongRangeWeapon;
                bool ignoresRocks = ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "mortar_t1");
                bool tileIsWalkable = tileComponent != null && tileComponent.isWalkable;

                int distance = step + 1;
                int distanceDamageModifier = ship.shipInfo.GetDistanceDamageModifier(distance);
                int totalDamageModifier = obstacleDamageModifier + distanceDamageModifier;

                if (shipComponent != null)
                {
                    TryAddReachableTarget(shipComponent, totalDamageModifier, distance, obstacleDamageModifier, lineType, side);
                }
                else if (tileIsWalkable)
                {
                    ReachablePositionsDamageModifiers.TryAdd(tileComponent.transform.position, totalDamageModifier);
                }
                else if (usingLongRange && !tileIsWalkable)
                {
                    if (!ignoresRocks)
                        obstacleDamageModifier -= 2;
                }
                else if (!tileIsWalkable)
                {
                    break;
                }
            }
        }

        if (ship.shipInfo.HasActivePassive(ship.shipInfo.WeaponModule, "sea_horse_t2"))
            AddSeaHorseRadiusTargets();
    }

    private void TryAddReachableTarget(Ship targetShip, int damageModifier, int distance, int coverModifier,
        IronTideAttackLineType lineType, int lineSide)
    {
        if (targetShip == null || targetShip == ship || targetShip.shipInfo == null || targetShip.shipInfo.Sunk)
            return;

        Vector3 targetPosition = targetShip.transform.position;
        ReachablePositionsDamageModifiers.TryAdd(targetPosition, damageModifier);
        ReachableTargetsDamageModifiers.TryAdd(targetPosition, damageModifier);
        ReachableTargetsDistance.TryAdd(targetPosition, distance);
        ReachableTargetsCoverModifiers.TryAdd(targetPosition, coverModifier);
        ReachableTargetsLineTypes.TryAdd(targetPosition, lineType);
        ReachableTargetsLineSides.TryAdd(targetPosition, lineSide);
    }

    private void AddSeaHorseRadiusTargets()
    {
        ShipInfo[] ships = FindObjectsByType<ShipInfo>(FindObjectsSortMode.None);
        for (int i = 0; i < ships.Length; i++)
        {
            ShipInfo candidate = ships[i];
            if (candidate == null || candidate == ship.shipInfo || candidate.Sunk)
                continue;

            int tileDistance = EstimateTileDistance(transform.position, candidate.transform.position);
            if (tileDistance < 1 || tileDistance > 2)
                continue;

            int damageModifier = ship.shipInfo.GetDistanceDamageModifier(tileDistance);
            Vector3 targetPosition = candidate.transform.position;
            ReachablePositionsDamageModifiers.TryAdd(targetPosition, damageModifier);
            ReachableTargetsDamageModifiers.TryAdd(targetPosition, damageModifier);
            ReachableTargetsDistance.TryAdd(targetPosition, tileDistance);
            ReachableTargetsCoverModifiers.TryAdd(targetPosition, 0);
            ReachableTargetsLineTypes.TryAdd(targetPosition, IronTideAttackLineType.Unknown);
            ReachableTargetsLineSides.TryAdd(targetPosition, -1);
        }
    }

    private static IronTideAttackLineType GetAttackLineType(int side)
    {
        return side == 1 || side == 4 ? IronTideAttackLineType.Straight : IronTideAttackLineType.Diagonal;
    }

    private static bool AreShipsAdjacent(ShipInfo first, ShipInfo second)
    {
        if (first == null || second == null)
            return false;

        return EstimateTileDistance(first.transform.position, second.transform.position) == 1;
    }

    private static int EstimateTileDistance(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0f;
        float distance = delta.magnitude;
        float adjacentDistance = ShipMovement.DistanceBetweenTiles * 1.45f;

        if (distance <= adjacentDistance)
            return 1;

        if (distance <= adjacentDistance * 2f)
            return 2;

        return Mathf.CeilToInt(distance / Mathf.Max(0.01f, adjacentDistance));
    }

    private static int GetPlayerId(ShipInfo info)
    {
        if (info == null)
            return -1;

        TurnPlayerController controller = info.GetComponent<TurnPlayerController>();
        return controller != null ? controller.playerID : -1;
    }
}
