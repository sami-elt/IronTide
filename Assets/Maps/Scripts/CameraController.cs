using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform mapParent; // dra in din "World" här
    Vector3 targetPosition;
    bool isMoving = false;
    [Header("Zoom")]
    public float zoomSpeed = 10f;
    public float minZoom = 10f;
    public float maxZoom = 40f;

    [Header("Movement")]
    public float moveSpeed = 20f;
    public float mapLimit = 100f;

    Camera cam;
    public Vector3 cameraOffset = new Vector3(0, 0, -10f);
    void Start()
    {
        cam = GetComponent<Camera>();
        FitCameraToMap();
    }

    void Update()
    {
        Zoom();
        //Move();

        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 3f);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
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
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 30f, 80f);
        }
    }

    //void Move()
    //{
    //    float h = Input.GetAxis("Horizontal"); // A/D
    //    float v = Input.GetAxis("Vertical");   // W/S

    //    Vector3 dir = new Vector3(h, 0, v);

    //    transform.position += dir * moveSpeed * Time.deltaTime;

    //    // Begränsa kameran till kartan
    //    Vector3 pos = transform.position;
    //    pos.x = Mathf.Clamp(pos.x, -mapLimit, mapLimit);
    //    pos.z = Mathf.Clamp(pos.z, -mapLimit, mapLimit);

    //    transform.position = pos;
    //}
    void FitCameraToMap()
    {
        Renderer[] renderers = mapParent.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
        {
            bounds.Encapsulate(r.bounds);
        }

        // centrera kameran
        Vector3 center = bounds.center;
        transform.position = new Vector3(center.x, transform.position.y, center.z);

        // räkna ut rätt zoom (Orthographic)
        float mapWidth = bounds.size.x;
        float mapHeight = bounds.size.z;

        float aspect = (float)Screen.width / Screen.height;

        float sizeBasedOnWidth = mapWidth / aspect / 2f;
        float sizeBasedOnHeight = mapHeight / 2f;

        float targetSize = Mathf.Max(sizeBasedOnWidth, sizeBasedOnHeight);

        Camera cam = GetComponent<Camera>();
        cam.orthographicSize = targetSize * 1.1f; // lite extra padding
    }

    public void MoveToPosition(Vector3 target)
    {
        Vector3 finalPos = target + cameraOffset;

        finalPos.y = transform.position.y; // behåll höjd

        targetPosition = finalPos;
        isMoving = true;
    }
}