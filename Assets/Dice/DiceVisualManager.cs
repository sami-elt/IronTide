using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceVisualManager : MonoBehaviour
{
    public static DiceVisualManager Instance { get; private set; }

    [Header("Dice Prefabs")]
    [SerializeField] private GameObject d4Prefab;
    [SerializeField] private GameObject d6Prefab;
    [SerializeField] private GameObject d8Prefab;
    [SerializeField] private GameObject d10Prefab;
    [SerializeField] private GameObject d12Prefab;
    [SerializeField] private GameObject d20Prefab;

    [Header("Display")]
    [SerializeField] private Transform displayPoint;
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float visibleDuration = 0.9f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private Vector3 fallbackOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private DiceFaceRotations[] faceRotations =
    {
        new DiceFaceRotations(4, new Vector3[4]),
        new DiceFaceRotations(6, new[]
        {
            new Vector3(-90f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, -90f),
            new Vector3(0f, 0f, 90f),
            new Vector3(180f, 0f, 0f),
            new Vector3(90f, 0f, 0f)
        }),
        new DiceFaceRotations(8, new Vector3[8]),
        new DiceFaceRotations(10, new Vector3[10]),
        new DiceFaceRotations(12, new Vector3[12]),
        new DiceFaceRotations(20, new Vector3[20])
    };

    private GameObject activeDice;
    private Coroutine activeRoll;
    private readonly Queue<RollRequest> pendingRolls = new Queue<RollRequest>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static void ShowRoll(int sides, int result, Transform source = null)
    {
        if (Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("DiceVisualManager");
            if (prefab != null)
                Instantiate(prefab);
        }

        if (Instance == null)
            return;

        Instance.QueueRoll(sides, result, source);
    }

    public static void HideActiveRoll()
    {
        if (Instance != null)
            Instance.Hide();
    }

    public void PlayRoll(int sides, int result, Transform source = null)
    {
        QueueRoll(sides, result, source);
    }

    public void PreviewResult(int sides, int result)
    {
        Hide();

        GameObject prefab = GetPrefab(sides);
        if (prefab == null)
            return;

        activeDice = Instantiate(prefab, GetDisplayPosition(null), Quaternion.Euler(GetFaceRotation(sides, result)));
    }

    public void QueueRoll(int sides, int result, Transform source = null)
    {
        GameObject prefab = GetPrefab(sides);
        if (prefab == null)
            return;

        pendingRolls.Enqueue(new RollRequest(prefab, sides, result, source));

        if (activeRoll == null)
            activeRoll = StartCoroutine(ProcessRollQueue());
    }

    public void Hide()
    {
        pendingRolls.Clear();

        if (activeRoll != null)
        {
            StopCoroutine(activeRoll);
            activeRoll = null;
        }

        ClearActiveDice();
    }

    private IEnumerator ProcessRollQueue()
    {
        while (pendingRolls.Count > 0)
        {
            RollRequest request = pendingRolls.Dequeue();
            yield return RollRoutine(request.Prefab, request.Sides, request.Result, request.Source);
        }

        activeRoll = null;
    }

    private void ClearActiveDice()
    {
        if (activeDice != null)
            Destroy(activeDice);

        activeDice = null;
    }

    private IEnumerator RollRoutine(GameObject prefab, int sides, int result, Transform source)
    {
        ClearActiveDice();

        Vector3 position = GetDisplayPosition(source);
        activeDice = Instantiate(prefab, position, Quaternion.identity);

        float timer = rollDuration;
        while (timer > 0f && activeDice != null)
        {
            activeDice.transform.Rotate(Vector3.one * spinSpeed * Time.deltaTime, Space.Self);
            timer -= Time.deltaTime;
            yield return null;
        }

        if (activeDice != null)
            activeDice.transform.rotation = Quaternion.Euler(GetFaceRotation(sides, result));

        yield return new WaitForSeconds(visibleDuration);
        ClearActiveDice();
    }

    private Vector3 GetDisplayPosition(Transform source)
    {
        if (displayPoint != null)
            return displayPoint.position;

        if (source != null)
            return source.position + fallbackOffset;

        return transform.position;
    }

    private GameObject GetPrefab(int sides)
    {
        switch (sides)
        {
            case 4:
                return d4Prefab;
            case 6:
                return d6Prefab;
            case 8:
                return d8Prefab;
            case 10:
                return d10Prefab;
            case 12:
                return d12Prefab;
            case 20:
                return d20Prefab;
            default:
                return d6Prefab;
        }
    }

    private Vector3 GetFaceRotation(int sides, int result)
    {
        if (faceRotations != null)
        {
            for (int i = 0; i < faceRotations.Length; i++)
            {
                if (faceRotations[i].Sides == sides && faceRotations[i].TryGetRotation(result, out Vector3 rotation))
                    return rotation;
            }
        }

        return Vector3.zero;
    }

    private readonly struct RollRequest
    {
        public readonly GameObject Prefab;
        public readonly int Sides;
        public readonly int Result;
        public readonly Transform Source;

        public RollRequest(GameObject prefab, int sides, int result, Transform source)
        {
            Prefab = prefab;
            Sides = sides;
            Result = result;
            Source = source;
        }
    }

    [System.Serializable]
    private class DiceFaceRotations
    {
        [SerializeField] private int sides;
        [SerializeField] private Vector3[] resultRotations;

        public int Sides => sides;

        public DiceFaceRotations(int sides, Vector3[] resultRotations)
        {
            this.sides = sides;
            this.resultRotations = resultRotations;
        }

        public bool TryGetRotation(int result, out Vector3 rotation)
        {
            rotation = Vector3.zero;

            if (resultRotations == null || result <= 0 || result > resultRotations.Length)
                return false;

            rotation = resultRotations[result - 1];
            return true;
        }
    }
}
