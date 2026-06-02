using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceVisualManager : MonoBehaviour
{
    public static DiceVisualManager Instance { get; private set; }


    [SerializeField] private GameObject d4Prefab;
    [SerializeField] private GameObject d6Prefab;
    [SerializeField] private GameObject d8Prefab;
    [SerializeField] private GameObject d10Prefab;
    [SerializeField] private GameObject d12Prefab;
    [SerializeField] private GameObject d20Prefab;


    [SerializeField] private Transform displayPoint;
    [SerializeField] private float rollDuration = 0.6f;
    [SerializeField] private float visibleDuration = 0.9f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private float groupedDiceSpacing = 0.75f;
    [SerializeField] private Vector3 fallbackOffset = new Vector3(0f, 2.2f, 0f);

    [SerializeField]
    private DiceFaceRotations[] faceRotations =
    {
        new DiceFaceRotations(4, new[]
        {
            new Vector3(0f, 0f, 0f),      // Rotation för resultat 1
            new Vector3(-109.5f, 0f, 0f), // Rotation för resultat 2
            new Vector3(109.5f, 90f, 0f), // Rotation för resultat 3
            new Vector3(109.5f, -90f, 0f) // Rotation för resultat 4
        }),
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

    //Roll logic

    public static int RollDice(int sides)
    {
        if (sides <= 0)
            sides = 6;

        int result = Random.Range(1, sides + 1);
        Debug.Log("Dice d" + sides + " result: " + result);
        return result;
    }

    public static int RollD4() => RollDice(4);
    public static int RollD6() => RollDice(6);
    public static int RollD8() => RollDice(8);
    public static int RollD10() => RollDice(10);
    public static int RollD12() => RollDice(12);
    public static int RollD20() => RollDice(20);

    // Rullar och visar animationen
    public static int RollAndShow(int sides, Transform source = null)
    {
        int result = RollDice(sides);
        ShowRoll(sides, result, source);
        return result;
    }

    public static int RollAndShow(int sides, int count, Transform source = null)
    {
        count = Mathf.Max(1, count);

        int total = 0;
        int[] results = new int[count];
        for (int i = 0; i < count; i++)
        {
            results[i] = RollDice(sides);
            total += results[i];
        }

        ShowRolls(sides, results, source);
        return total;
    }

    //Visuals

    public static void ShowRoll(int sides, int result, Transform source = null)
    {
        ShowRolls(sides, new[] { result }, source);
    }

    public static void ShowRolls(int sides, IList<int> results, Transform source = null)
    {
        if (Instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("DiceVisualManager");
            if (prefab != null)
                Instantiate(prefab);
        }

        if (Instance == null)
            return;

        Instance.QueueRolls(sides, results, source);
    }

    public static void HideActiveRoll()
    {
        if (Instance != null)
            Instance.Hide();
    }

    public void QueueRoll(int sides, int result, Transform source = null)
    {
        QueueRolls(sides, new[] { result }, source);
    }

    public void QueueRolls(int sides, IList<int> results, Transform source = null)
    {
        GameObject prefab = GetPrefab(sides);
        if (prefab == null || results == null || results.Count == 0)
            return;

        int[] resultCopy = new int[results.Count];
        for (int i = 0; i < results.Count; i++)
            resultCopy[i] = Mathf.Clamp(results[i], 1, Mathf.Max(1, sides));

        pendingRolls.Enqueue(new RollRequest(prefab, sides, resultCopy, source));

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
            yield return RollRoutine(request.Prefab, request.Sides, request.Results, request.Source);
        }

        activeRoll = null;
    }

    private void ClearActiveDice()
    {
        if (activeDice != null)
            Destroy(activeDice);

        activeDice = null;
    }

    private IEnumerator RollRoutine(GameObject prefab, int sides, int[] results, Transform source)
    {
        ClearActiveDice();

        Vector3 position = GetDisplayPosition(source);
        activeDice = new GameObject($"Dice Roll {results.Length}xD{sides}");
        activeDice.transform.position = position;

        for (int i = 0; i < results.Length; i++)
        {
            Vector3 offset = GetGroupedDiceOffset(i, results.Length);
            GameObject dice = Instantiate(prefab, position + offset, Quaternion.identity, activeDice.transform);
            DisableRaycastColliders(dice);
        }

        DisableRaycastColliders(activeDice);

        float timer = rollDuration;
        while (timer > 0f && activeDice != null)
        {
            for (int i = 0; i < activeDice.transform.childCount; i++)
                activeDice.transform.GetChild(i).Rotate(Vector3.one * spinSpeed * Time.deltaTime, Space.Self);

            timer -= Time.deltaTime;
            yield return null;
        }

        if (activeDice != null)
        {
            for (int i = 0; i < activeDice.transform.childCount && i < results.Length; i++)
                activeDice.transform.GetChild(i).rotation = Quaternion.Euler(GetFaceRotation(sides, results[i]));
        }

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

    private Vector3 GetGroupedDiceOffset(int index, int count)
    {
        if (count <= 1)
            return Vector3.zero;

        float centeredIndex = index - (count - 1) * 0.5f;
        return new Vector3(centeredIndex * groupedDiceSpacing, 0f, 0f);
    }

    private static void DisableRaycastColliders(GameObject dice)
    {
        if (dice == null)
            return;

        Collider[] colliders = dice.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private GameObject GetPrefab(int sides)
    {
        switch (sides)
        {
            case 4: return d4Prefab != null ? d4Prefab : d6Prefab;
            case 6: return d6Prefab;
            case 8: return d8Prefab != null ? d8Prefab : d6Prefab;
            case 10: return d10Prefab != null ? d10Prefab : d6Prefab;
            case 12: return d12Prefab != null ? d12Prefab : d6Prefab;
            case 20: return d20Prefab != null ? d20Prefab : d6Prefab;
            default: return d6Prefab;
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
        public readonly int[] Results;
        public readonly Transform Source;

        public RollRequest(GameObject prefab, int sides, int[] results, Transform source)
        {
            Prefab = prefab;
            Sides = sides;
            Results = results;
            Source = source;
        }
    }

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
