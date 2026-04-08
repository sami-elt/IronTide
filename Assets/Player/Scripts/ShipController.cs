using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private Ship ship;

    private void Start()
    {
        ship = GetComponent<Ship>();
    }

    public void VisualizeReachableTiles()
    {

    }

    public void VisualizeWeaponRange()
    {

    }
}
