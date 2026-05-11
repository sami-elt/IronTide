using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    public GenerateMap map;
    public CameraController cam;

    int currentIndex = 0;

    void Start()
    {
        // vänta tills map är genererad
        Invoke(nameof(Init), 0.1f);
    }

    void Init()
    {
        MoveCamera();
    }

    void Update()
    {
        // 🔥 tryck SPACE för att byta "tur"
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Next();
        }
    }

    void Next()
    {
        currentIndex++;

        if (currentIndex >= map.spawnWorldPositions.Length)
            currentIndex = 0;

        MoveCamera();
    }

    void MoveCamera()
    {
        if (map.spawnWorldPositions == null || map.spawnWorldPositions.Length == 0)
            return;

        Vector3 pos = map.spawnWorldPositions[currentIndex];

        cam.MoveToPosition(pos);
    }
}