using System.Collections;
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

    private GameObject activeDice;
    private Coroutine activeRoll;

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
            DiceVisualManager prefab = Resources.Load<DiceVisualManager>("DiceVisualManager");
            if (prefab != null)
                Instantiate(prefab);
        }

        if (Instance == null)
            return;

        Instance.PlayRoll(sides, result, source);
    }

    public void PlayRoll(int sides, int result, Transform source = null)
    {
        GameObject prefab = GetPrefab(sides);
        if (prefab == null)
            return;

        if (activeRoll != null)
            StopCoroutine(activeRoll);

        activeRoll = StartCoroutine(RollRoutine(prefab, sides, result, source));
    }

    public void Hide()
    {
        if (activeRoll != null)
        {
            StopCoroutine(activeRoll);
            activeRoll = null;
        }

        if (activeDice != null)
            Destroy(activeDice);
    }

    private IEnumerator RollRoutine(GameObject prefab, int sides, int result, Transform source)
    {
        if (activeDice != null)
            Destroy(activeDice);

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
        Hide();
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

    private static Vector3 GetFaceRotation(int sides, int result)
    {
        if (sides == 6)
        {
            switch (result)
            {
                case 1:
                    return new Vector3(0f, 0f, 0f);
                case 2:
                    return new Vector3(90f, 0f, 0f);
                case 3:
                    return new Vector3(0f, 0f, -90f);
                case 4:
                    return new Vector3(0f, 0f, 90f);
                case 5:
                    return new Vector3(-90f, 0f, 0f);
                case 6:
                    return new Vector3(180f, 0f, 0f);
                default:
                    return Vector3.zero;
            }
        }

        return new Vector3(0f, (result - 1) * (360f / Mathf.Max(1, sides)), 0f);
    }
}
