using UnityEngine;

public class ShipToggle : MonoBehaviour
{
    //Allows easy toggle of ship scripts by disabling or enabling this script

    private ShipInfo shipInfo;
    private ShipMovement shipMovement;
    private ShipWeapon shipWeapon;
    

    private void Start()
    {
        shipInfo = GetComponent<ShipInfo>();
        shipMovement = GetComponent<ShipMovement>();
        shipWeapon = GetComponent<ShipWeapon>();
        
    }

    private void OnDisable()
    {
        shipInfo.enabled = false;
        shipMovement.enabled = false;
        shipWeapon.enabled = false;
    }

    private void OnEnable()
    {
        shipInfo.enabled = true;
        shipMovement.enabled = true;
        shipWeapon.enabled = true;
    }
}
