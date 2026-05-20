using UnityEngine;

public class ReachVisual : MonoBehaviour
{
    //Rotates gameObject with camera rotation
    private GameObject target;

    private void Start()
    {
        target = Camera.main.gameObject;
    }

    private void Update()
    {
        Vector3 eulerRot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(new(eulerRot.x, target.transform.rotation.eulerAngles.y, eulerRot.z));   
    }
}
