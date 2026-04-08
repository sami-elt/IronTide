using UnityEngine;

public class Ship : MonoBehaviour
{
    //Allows easy toggle of ship scripts by disabling or enabling this script

    public ShipInfo shipInfo;
    public ShipMovement shipMovement;
    public ShipWeapon shipWeapon;
    

    private void Awake()
    {
        shipInfo = GetComponent<ShipInfo>();
        shipMovement = GetComponent<ShipMovement>();
        shipWeapon = GetComponent<ShipWeapon>();

        if (enabled == false)
        {
            Disable();
        }
    }

    private void Disable()
    {
        shipInfo.enabled = false;
        shipMovement.enabled = false;
        shipWeapon.enabled = false;
    }

    private void OnDisable()
    {
        Disable();
    }

    private void OnEnable()
    {
        shipInfo.enabled = true;
        shipMovement.enabled = true;
        shipWeapon.enabled = true;
    }
}
