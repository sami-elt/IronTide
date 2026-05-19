using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform mapParent; // dra in din "World" här
    Vector3 targetPosition;
    bool isMoving = false;

    [Header("View")]
    public bool useThirdPersonPerspective = false;
    public bool followTurnManagerPlayers = true;
    public bool faceMapCenter = true;
    [Range(45f, 75f)] public float perspectivePitch = 58f;
    public float perspectiveYaw = 0f;
    [Range(35f, 65f)] public float perspectiveFieldOfView = 46f;
    public float perspectiveFollowDistance = 22f;
    public float targetHeight = 0.8f;
    public float lookAheadDistance = 4f;
    [Range(0f, 0.35f)] public float bottomViewportReserve = 0.16f;
    [Range(1f, 1.4f)] public float mapFramePadding = 1.08f;

    [Header("Strategy View")]
    public bool strategyTopDownActive = false;
    public float topDownHeight = 55f;
    [Range(60f, 85f)] public float topDownPitch = 72f;
    public float topDownYaw = 0f;
    [Range(35f, 65f)] public float topDownFieldOfView = 48f;
    [Range(1f, 1.4f)] public float topDownMapPadding = 1.12f;

    [Header("Zoom")]
    public float zoomSpeed = 10f;
    public float minZoom = 10f;
    public float maxZoom = 40f;
    public float minFieldOfView = 35f;
    public float maxFieldOfView = 65f;

    [Header("Movement")]
    public float moveSpeed = 20f;
    public float mapLimit = 100f;
    public float followSmoothTime = 0.55f;
    public float rotationSmoothSpeed = 5f;

    Camera cam;
    public Vector3 cameraOffset = new Vector3(0, 0, -10f);
    Quaternion targetRotation;
    Vector3 moveVelocity;
    Transform followedTarget;

    public bool IsStrategyTopDownActive => strategyTopDownActive;

    private void OnEnable()
    {
        TurnManager.OnTurnStarted += HandleTurnStarted;
    }

    private void OnDisable()
    {
        TurnManager.OnTurnStarted -= HandleTurnStarted;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        ApplyViewPreset();
        FitCameraToMap();

        if (strategyTopDownActive)
            SetTopDownStrategyView(true);
        else
            FocusCurrentTurnPlayer(true);
    }

    void Update()
    {
        Zoom();
        Move();

        if (!strategyTopDownActive)
        {
            RefreshFollowTargetFromTurnManager(false);

            if (useThirdPersonPerspective && followedTarget != null)
            {
                GetThirdPersonCameraPose(followedTarget.position, followedTarget, out targetPosition, out targetRotation);
                isMoving = true;
            }
        }

        if (isMoving)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref moveVelocity, followSmoothTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSmoothSpeed);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f &&
                Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            {
                isMoving = false;
            }
        }
    }

    void Zoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (cam.orthographic)
        {
            float target = cam.orthographicSize - scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, target, Time.deltaTime * 10f);
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            float target = cam.fieldOfView - scroll * zoomSpeed;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, Time.deltaTime * 10f);
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFieldOfView, maxFieldOfView);
        }
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal"); // A/D
        float v = Input.GetAxis("Vertical");   // W/S

        Vector3 dir = new Vector3(h, 0, v);

        transform.position += dir * moveSpeed * Time.deltaTime;

        // 🔒 Begränsa kameran till kartan
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -mapLimit, mapLimit);
        pos.z = Mathf.Clamp(pos.z, -mapLimit, mapLimit);

        transform.position = pos;
    }
    void FitCameraToMap()
    {
        if (!TryGetMapBounds(out Bounds bounds))
            return;

        if (strategyTopDownActive)
        {
            SetTopDownStrategyView(true);
            return;
        }

        if (useThirdPersonPerspective)
        {
            FramePerspectiveBounds(bounds);
            return;
        }

        // räkna ut rätt zoom (Orthographic)
        float mapWidth = bounds.size.x;
        float mapHeight = bounds.size.z;

        float aspect = (float)Screen.width / Screen.height;

        float sizeBasedOnWidth = mapWidth / aspect / 2f;
        float sizeBasedOnHeight = mapHeight / 2f;

        float targetSize = Mathf.Max(sizeBasedOnWidth, sizeBasedOnHeight);

        Camera cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = targetSize * 1.1f; // lite extra padding

        // centrera kameran
        Vector3 center = bounds.center;
        transform.position = new Vector3(center.x, transform.position.y, center.z);
    }

    public void MoveToPosition(Vector3 target)
    {
        if (strategyTopDownActive)
        {
            SetTopDownStrategyView(false);
            return;
        }

        ApplyViewPreset();
        followedTarget = null;

        Vector3 finalPos;
        Quaternion finalRotation = transform.rotation;
        if (useThirdPersonPerspective)
        {
            GetThirdPersonCameraPose(target, null, out finalPos, out finalRotation);
        }
        else
        {
            finalPos = target + cameraOffset;
            finalPos.y = transform.position.y; // behåll höjd
        }

        targetPosition = finalPos;
        targetRotation = finalRotation;
        isMoving = true;
    }

    public void MoveToTarget(Transform target)
    {
        MoveToTarget(target, false);
    }

    public void MoveToTarget(Transform target, bool snap)
    {
        if (target == null)
            return;

        if (strategyTopDownActive)
            return;

        ApplyViewPreset();

        if (!useThirdPersonPerspective)
        {
            MoveToPosition(target.position);
            return;
        }

        followedTarget = target;
        GetThirdPersonCameraPose(target.position, target, out targetPosition, out targetRotation);
        if (snap)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            moveVelocity = Vector3.zero;
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }
    }

    public void FocusCurrentTurnPlayer(bool snap)
    {
        if (strategyTopDownActive)
            return;

        RefreshFollowTargetFromTurnManager(snap);
    }

    public void ToggleStrategyView()
    {
        if (strategyTopDownActive)
            SetThirdPersonView(false);
        else
            SetTopDownStrategyView(true);
    }

    public void SetThirdPersonView(bool snap)
    {
        strategyTopDownActive = false;
        useThirdPersonPerspective = true;
        ApplyViewPreset();
        FocusCurrentTurnPlayer(snap);
    }

    public void SetTopDownStrategyView(bool snap)
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            return;

        strategyTopDownActive = true;
        followedTarget = null;

        if (!TryGetMapBounds(out Bounds bounds))
            bounds = new Bounds(transform.position, Vector3.one * 10f);

        cam.orthographic = false;
        cam.fieldOfView = topDownFieldOfView;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1000f);

        GetTopDownCameraPose(bounds, out targetPosition, out targetRotation);

        if (snap)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            moveVelocity = Vector3.zero;
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }
    }

    void ApplyViewPreset()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            return;

        if (strategyTopDownActive || !useThirdPersonPerspective)
            return;

        cam.orthographic = false;
        cam.fieldOfView = perspectiveFieldOfView;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1000f);
    }

    Quaternion GetThirdPersonRotation()
    {
        return Quaternion.Euler(perspectivePitch, perspectiveYaw, 0f);
    }

    Vector3 GetThirdPersonOffset()
    {
        Vector3 forward = GetThirdPersonRotation() * Vector3.forward;
        return -forward * perspectiveFollowDistance;
    }

    void GetThirdPersonCameraPose(Vector3 target, Transform targetTransform, out Vector3 position, out Quaternion rotation)
    {
        Vector3 viewDirection = GetThirdPersonViewDirection(target, targetTransform);
        float pitchRadians = perspectivePitch * Mathf.Deg2Rad;
        float horizontalDistance = Mathf.Cos(pitchRadians) * perspectiveFollowDistance;
        float height = Mathf.Sin(pitchRadians) * perspectiveFollowDistance;

        Vector3 lookTarget = target + Vector3.up * targetHeight + viewDirection * lookAheadDistance;
        position = target - viewDirection * horizontalDistance + Vector3.up * height;
        rotation = Quaternion.LookRotation(lookTarget - position, Vector3.up);
    }

    Vector3 GetThirdPersonViewDirection(Vector3 target, Transform targetTransform)
    {
        Vector3 direction = Vector3.zero;

        if (faceMapCenter && TryGetMapCenter(out Vector3 mapCenter))
        {
            direction = mapCenter - target;
        }
        else if (targetTransform != null)
        {
            direction = targetTransform.forward;
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            direction = Quaternion.Euler(0f, perspectiveYaw, 0f) * Vector3.forward;

        return direction.normalized;
    }

    bool TryGetMapCenter(out Vector3 center)
    {
        center = Vector3.zero;
        if (!TryGetMapBounds(out Bounds bounds))
            return false;

        center = bounds.center;
        return true;
    }

    bool TryGetMapBounds(out Bounds bounds)
    {
        bounds = default;
        if (mapParent == null)
            return false;

        Renderer[] renderers = mapParent.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            bounds = new Bounds(mapParent.position, Vector3.one);
            return true;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    void GetTopDownCameraPose(Bounds bounds, out Vector3 position, out Quaternion rotation)
    {
        Vector3 target = bounds.center;
        target.y = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.35f);

        rotation = Quaternion.Euler(topDownPitch, topDownYaw, 0f);
        Vector3 forward = rotation * Vector3.forward;
        float largestMapAxis = Mathf.Max(bounds.size.x, bounds.size.z);
        float minDistance = Mathf.Max(topDownHeight, largestMapAxis * 0.5f);
        float maxDistance = Mathf.Max(topDownHeight + 20f, largestMapAxis * 10f);

        for (int i = 0; i < 24; i++)
        {
            float testDistance = (minDistance + maxDistance) * 0.5f;
            transform.position = target - forward * testDistance;
            transform.rotation = rotation;

            if (CameraBoundsFitViewport(bounds))
                maxDistance = testDistance;
            else
                minDistance = testDistance;
        }

        position = target - forward * (maxDistance * topDownMapPadding);
    }

    void FramePerspectiveBounds(Bounds bounds)
    {
        if (cam == null)
            return;

        ApplyViewPreset();

        Vector3 target = bounds.center;
        target.y = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.35f);

        Quaternion frameRotation = GetThirdPersonRotation();
        Vector3 forward = frameRotation * Vector3.forward;
        float largestMapAxis = Mathf.Max(bounds.size.x, bounds.size.z);
        float minDistance = Mathf.Max(perspectiveFollowDistance, largestMapAxis * 0.25f);
        float maxDistance = Mathf.Max(80f, largestMapAxis * 8f);

        for (int i = 0; i < 24; i++)
        {
            float testDistance = (minDistance + maxDistance) * 0.5f;
            transform.position = target - forward * testDistance;
            transform.rotation = frameRotation;

            if (CameraBoundsFitViewport(bounds))
                maxDistance = testDistance;
            else
                minDistance = testDistance;
        }

        float frameDistance = maxDistance * mapFramePadding;
        transform.position = target - forward * frameDistance;
        targetPosition = transform.position;
        targetRotation = frameRotation;
        transform.rotation = frameRotation;
    }

    bool CameraBoundsFitViewport(Bounds bounds)
    {
        if (cam == null)
            return false;

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        float minViewportY = bottomViewportReserve + 0.025f;

        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = new Vector3(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z);

                    Vector3 viewportPoint = cam.WorldToViewportPoint(corner);
                    if (viewportPoint.z <= 0f ||
                        viewportPoint.x < 0.025f ||
                        viewportPoint.x > 0.975f ||
                        viewportPoint.y < minViewportY ||
                        viewportPoint.y > 0.965f)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    void HandleTurnStarted(int playerId)
    {
        if (strategyTopDownActive)
            return;

        RefreshFollowTargetFromTurnManager(false);
    }

    void RefreshFollowTargetFromTurnManager(bool snap)
    {
        if (!followTurnManagerPlayers || TurnManager.Instance == null)
            return;

        TurnPlayerController currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        if (currentPlayer == null)
            return;

        if (!snap && followedTarget == currentPlayer.transform)
            return;

        MoveToTarget(currentPlayer.transform, snap);
    }
}
