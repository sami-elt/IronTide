using UnityEngine;

public class ReachVisual : MonoBehaviour
{
    //For rotating towards camera
    private GameObject target;

    private void Start()
    {
        target = Camera.main.gameObject;
    }

    private void Update()
    {
        Vector3 targetPosition = target.transform.forward + target.transform.position;
        targetPosition.y = target.transform.position.y;
        Vector3 targetRotation = (targetPosition - target.transform.position).normalized;
        
        transform.rotation = Quaternion.LookRotation(targetRotation, Vector3.up);
    }
}
