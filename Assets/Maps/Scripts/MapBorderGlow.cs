using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MapBorderGlow : MonoBehaviour
{
    public float radius = 32f;
    public int segments = 6;
    public float height = 0.2f;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();

        line.loop = true;
        line.useWorldSpace = false;

        line.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i);

            float x = radius * Mathf.Cos(angle);
            float z = radius * Mathf.Sin(angle);

            line.SetPosition(i, new Vector3(x, height, z));
        }
    }
}