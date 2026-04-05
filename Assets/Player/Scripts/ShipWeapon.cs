using UnityEngine;

public class ShipWeapon : MonoBehaviour
{
    [SerializeField] ShipInfo ship;

    ShipInfo target;

    private void Awake()
    {
        ship = GetComponent<ShipInfo>();
    }

    public void Attack()
    {
        if (target != null)
        {
            target.Hurt(ship.GetWeaponDamage());
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
