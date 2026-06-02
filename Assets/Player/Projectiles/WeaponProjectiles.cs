using UnityEngine;

public class WeaponProjectiles : MonoBehaviour
{
    public float speed = 15f;
    public GameObject hitEffectPrefab;

    [SerializeField] private float targetHeightOffset = 0.8f;
    [SerializeField] private float hitDistance = 0.35f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float arcHeight = 0.35f;
    [SerializeField] private bool spreadChildProjectiles;
    [SerializeField] private float childSpread = 0.35f;

    private Transform target;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 lastPosition;
    private Transform[] childProjectiles;
    private Vector3[] childStartPositions;
    private float timeAlive;
    private float travelTime = 1f;

    public void Shoot(Transform targetTransform)
    {
        target = targetTransform;
        startPosition = transform.position;
        lastPosition = startPosition;

        if (target != null)
        {
            targetPosition = GetTargetPosition();
            float distance = Vector3.Distance(startPosition, targetPosition);
            travelTime = Mathf.Max(distance / Mathf.Max(speed, 0.1f), 0.05f);
            transform.LookAt(targetPosition);
            CacheChildProjectiles();
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

        float progress = Mathf.Clamp01(timeAlive / travelTime);
        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        nextPosition.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

        transform.position = nextPosition;

        Vector3 direction = transform.position - lastPosition;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        UpdateChildSpread(progress);
        lastPosition = transform.position;

        if (progress >= 1f || Vector3.Distance(transform.position, targetPosition) < hitDistance)
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

    private void CacheChildProjectiles()
    {
        if (!spreadChildProjectiles || transform.childCount == 0)
            return;

        childProjectiles = new Transform[transform.childCount];
        childStartPositions = new Vector3[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            childProjectiles[i] = transform.GetChild(i);
            childStartPositions[i] = childProjectiles[i].localPosition;
        }
    }

    private void UpdateChildSpread(float progress)
    {
        if (childProjectiles == null || childProjectiles.Length == 0)
            return;

        float spreadProgress = Mathf.SmoothStep(0f, 1f, progress);
        float angleStep = 360f / childProjectiles.Length;

        for (int i = 0; i < childProjectiles.Length; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 spreadDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.45f, 0f);
            childProjectiles[i].localPosition = childStartPositions[i] + spreadDirection * childSpread * spreadProgress;
        }
    }
}
