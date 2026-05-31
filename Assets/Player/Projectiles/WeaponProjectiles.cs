using UnityEngine;

public class WeaponProjectiles : MonoBehaviour
{
    public float speed = 15f;
    public GameObject hitEffectPrefab;

    [SerializeField] private float targetHeightOffset = 0.8f;
    [SerializeField] private float hitDistance = 0.35f;
    [SerializeField] private float lifeTime = 5f;

    private Transform target;
    private float timeAlive;

    public void Shoot(Transform targetTransform)
    {
        target = targetTransform;

        if (target != null)
        {
            transform.LookAt(GetTargetPosition());
        }
    }

    private void Update()
    {
        timeAlive += Time.deltaTime;

        if (target == null || timeAlive >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = GetTargetPosition();

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        if (Vector3.Distance(transform.position, targetPosition) < hitDistance)
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, targetPosition, Quaternion.identity);

            }
            Destroy(gameObject);
        }
    }

    private Vector3 GetTargetPosition()
    {
        return target.position + Vector3.up * targetHeightOffset;
    }
}