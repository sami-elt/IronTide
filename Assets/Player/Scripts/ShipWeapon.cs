using UnityEngine;
using System.Collections.Generic;
using IronTide.BasicCards;

public class ShipWeapon : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private ShipInfo target;

    public bool HasAttacked { get; private set; }

    public Dictionary<Vector3, int> ReachableTargetsDamageReduction = new();

    [SerializeField] GameObject bulletObject;

    private void Awake()
    {
        ship = GetComponent<Ship>();
    }


    public void EnterAttackPhase()
    {
        HasAttacked = false;
        FindReachableTargets();
    }

    public void Attack(int damageReduction)
    {
        if (target != null)
        {
            target.Hurt(ship.shipInfo.GetWeaponDamage() - damageReduction);
            target = null;
            HasAttacked = true;

            //Vector3 bulletPosition = transform.position;
            //Quaternion bulletRotation = Quaternion.FromToRotation(Vector3.forward, Vector3.Normalize(target.transform.position - bulletPosition));
            //Instantiate(bulletObject, bulletPosition, bulletRotation);
            
        }

    }

    public void SelectTarget(GameObject targetObject)
    {
        if (targetObject.TryGetComponent<ShipInfo>(out var newTarget))
        {
            target = newTarget;
        }
    }

    //Goes through the straight paths the player can take and saves the position and reduced damage of possible targets.
    private void FindReachableTargets()
    {
        ReachableTargetsDamageReduction.Clear();

        for (int side = 0; side < 6; side++)
        {
            Vector3 origin = transform.position;
            Vector3 direction = Quaternion.AngleAxis(30 + side * 60, Vector3.up) * Vector3.forward;
            float tileSize = ship.shipMovement.distanceBetweenTiles;
            int damageReduction = 0;

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
                bool tileIsWalkable = tileComponent != null && tileComponent.isWalkable;

                if (shipComponent != null)
                {
                    ReachableTargetsDamageReduction.TryAdd(shipComponent.transform.position, damageReduction);
                }
                else if (usingLongRange && !tileIsWalkable)
                {
                    damageReduction += 2;
                }
                else if(!tileIsWalkable)
                {
                    //Debug.LogWarning("Broke because of hitting an blocking tile");
                    break;
                }

            }
            //Debug.LogWarning("Reached end of side " + side);
        }
    }
}
