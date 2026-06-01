using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceManager : MonoBehaviour
{

    public GameObject diceD4;
    public GameObject diceD6;
    public GameObject diceD8;
    public GameObject diceD12;


    //Behövs fixas, varje animation för sig så rätt sida på tärningen visas
    private static readonly Dictionary<int, Vector3> D6FaceRotations = new()
{
    { 1, new Vector3(0, 0, 0) },    
    { 2, new Vector3(90, 0, 0) },
    { 3, new Vector3(0, 0, -90) },
    { 4, new Vector3(0, 0, 90) },
    { 5, new Vector3(-90, 0, 0) },
    { 6, new Vector3(180, 0, 0) },
};

    private static readonly Dictionary<int, Vector3> D4FaceRotations = new()
{
    { 1, new Vector3(0, 0, 0) },    
    { 2, new Vector3(0, 0, 0) },
    { 3, new Vector3(0, 0, 0) },
    { 4, new Vector3(0, 0, 0) },
};

    private static readonly Dictionary<int, Vector3> D8FaceRotations = new()
{
    { 1, new Vector3(0, 0, 0) },    
    { 2, new Vector3(0, 0, 0) },
    { 3, new Vector3(0, 0, 0) },
    { 4, new Vector3(0, 0, 0) },
    { 5, new Vector3(0, 0, 0) },
    { 6, new Vector3(0, 0, 0) },
    { 7, new Vector3(0, 0, 0) },
    { 8, new Vector3(0, 0, 0) },
};

    private static readonly Dictionary<int, Vector3> D12FaceRotations = new()
{
    { 1, new Vector3(0, 0, 0) },     
    { 2, new Vector3(0, 0, 0) },
    { 3, new Vector3(0, 0, 0) },
    { 4, new Vector3(0, 0, 0) },
    { 5, new Vector3(0, 0, 0) },
    { 6, new Vector3(0, 0, 0) },
    { 7, new Vector3(0, 0, 0) },
    { 8, new Vector3(0, 0, 0) },
    { 9, new Vector3(0, 0, 0) },
    { 10, new Vector3(0, 0, 0) },
    { 11, new Vector3(0, 0, 0) },
    { 12, new Vector3(0, 0, 0) },
};


    private int currentSides = 6;
    private bool isRolling = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HideAllDice();

        //ActiveDice(6);

        
    }

    public void ActiveDice(int sides)
    {

        currentSides = sides;
        HideAllDice();

        switch (sides)
        {
            case 4: if (diceD4 != null) diceD4.SetActive(true); break;
            case 6: if (diceD6 != null) diceD6.SetActive(true); break;
            case 8: if (diceD8 != null) diceD8.SetActive(true); break;
            case 12: if (diceD12 != null) diceD12.SetActive(true); break;
        }
        DisableActiveDiceColliders();
    }
    public void HideAllDice()
    {
        if (diceD4 != null) diceD4.SetActive(false);
        if (diceD6 != null) diceD6.SetActive(false);
        if (diceD8 != null) diceD8.SetActive(false);
        if (diceD12 != null) diceD12.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
    }

    public void RollForAttack(System.Action onComplete)
    {
        if (!isRolling)
            StartCoroutine(RollAttackAnimation(onComplete));
    }

    private IEnumerator RollAttackAnimation(System.Action onComplete)
    {
        isRolling = true;

        int sides = TurnManager.Instance.GetCurrentPlayer()
            .GetComponent<ShipInfo>().GetWeaponDiceSides();
        ActiveDice(sides);
        GameObject activeDiceObj = GetActiveDiceObject();


        //timer för animationen, ändra vid behov för att skapa en mer smooth upplevelse
        float timer = 0.5f;
        while (timer > 0)
        {
            if (activeDiceObj != null)
                activeDiceObj.transform.Rotate(new Vector3(800, 1200, 1000) * Time.deltaTime);
            timer -= Time.deltaTime;
            yield return null;
        }

        int result = Random.Range(1, sides + 1);
        if (activeDiceObj != null)
            activeDiceObj.transform.eulerAngles = GetFaceRotation(sides, result);

        //minska tiden efter attacken.
        yield return new WaitForSeconds(0.8f);
        HideAllDice();
        isRolling = false;

        onComplete?.Invoke();
    }

    public void RollForMovement(ShipMovement movement)
    {
        if (!isRolling)
            StartCoroutine(RollAnimation(movement));
    }


    private IEnumerator RollAnimation(ShipMovement shipToMove)
    {
        isRolling = true;

        GameObject activeDiceObj = GetActiveDiceObject();

        float timer = 1f;
        while (timer > 0)
        {
            if (activeDiceObj != null)
            {
            
                activeDiceObj.transform.Rotate(new Vector3(800, 1200, 1000) * Time.deltaTime);
            }
            timer -= Time.deltaTime;
            yield return null; 
        }

        int result = Random.Range(1, currentSides + 1);

        // Återställ rotationen
        if (activeDiceObj != null)
        {
            activeDiceObj.transform.eulerAngles = GetFaceRotation(currentSides, result);
        }

        shipToMove.ReceiveDiceResult(result);

        yield return new WaitForSeconds(2f);
        HideAllDice();

        isRolling = false;
    }

    private Vector3 GetFaceRotation(int sides, int result)
    {
        Dictionary<int, Vector3> table = sides switch
        {
            4 => D4FaceRotations,
            6 => D6FaceRotations,
            8 => D8FaceRotations,
            12 => D12FaceRotations,
            _ => D6FaceRotations
        };

        return table.TryGetValue(result, out Vector3 rot) ? rot : Vector3.zero;
    }
    private GameObject GetActiveDiceObject()
    {
        if (currentSides == 4) return diceD4;
        if (currentSides == 6) return diceD6;
        if (currentSides == 8) return diceD8;
        if (currentSides == 12) return diceD12;
        return diceD6;
    }

    private void DisableActiveDiceColliders()
    {
        GameObject activeDiceObj = GetActiveDiceObject();
        if (activeDiceObj == null)
            return;

        Collider[] colliders = activeDiceObj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }
}
