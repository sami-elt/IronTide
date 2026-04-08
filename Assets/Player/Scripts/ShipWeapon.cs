using UnityEngine;

public class ShipWeapon : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private ShipInfo target;

    private void Awake()
    {
        ship = GetComponent<Ship>();
    }

    public void Attack()
    {
        if (target != null)
        {
            target.Hurt(ship.shipInfo.GetWeaponDamage());
            target = null;
        }
            
    }

    public void SelectTarget(GameObject targetObject)
    {
        if (targetObject.TryGetComponent<ShipInfo>(out var newTarget))
        {
            target = newTarget;
        }
    }
}
