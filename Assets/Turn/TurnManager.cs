using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public TurnPlayerController[] Players;
    public int CurrentPlayerIndex = 0;

     public TurnPhase currentPhase;

     // Dice results
     private int movementRoll;
     private int attackRoll;

     private int totalMovement;
     private int totalAttack;

     private void Awake()
     {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
     }

    void Start()
    {
        StartTurn();
    }

    private void StartTurn()
    {
        // Reset all players
        for (int i = 0; i < Players.Length; i++)
        {
            Players[i].SetMyTurn(false);
        }   
        //Active player
        Players[CurrentPlayerIndex].SetMyTurn(true);

        //Reset dice rolls
        movementRoll = 0;
        attackRoll = 0;
        totalMovement = 0;
        totalAttack = 0;
        
        //Start first phase
        currentPhase = TurnPhase.RollMovement;

        Debug.Log("Player" + Players[CurrentPlayerIndex].playerID + " turn");
        Debug.Log("Current phase: " + currentPhase);
    }

    public void NextPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.RollMovement:
                currentPhase = TurnPhase.Move;
                Debug.Log("Move phase, Max steps: " + totalMovement);
                break;

            case TurnPhase.Move:
                currentPhase = TurnPhase.RollAttack;
                Debug.Log("Roll attack phase");
                break;

            case TurnPhase.RollAttack:
                currentPhase = TurnPhase.Attack;
                Debug.Log("Attack phase, Max damage: " + totalAttack);
                break;

            case TurnPhase.Attack:
                EndTurn();
                break;
        }
    }

    private void EndTurn()
    {
        Players[CurrentPlayerIndex].SetMyTurn(false);

        CurrentPlayerIndex++;

        if (CurrentPlayerIndex >= Players.Length)
        {
            CurrentPlayerIndex = 0;
        }

        StartTurn();
    }

    public TurnPlayerController GetCurrentPlayer()
    {
        return Players[CurrentPlayerIndex];
    }

    //From dice roll
    public void SetMovementRoll(int value)
    {
        movementRoll = value;

        int motorBonus = GetCurrentPlayer().motorBonus;
        totalMovement = movementRoll + motorBonus;

        Debug.Log("movement dice: " + movementRoll);
        Debug.Log("total movement: " + totalMovement);
    }

    public void SetAttackRoll(int value)
    {
        attackRoll = value;

        int weaponBonus = GetCurrentPlayer().weaponBonus;
        totalAttack = attackRoll + weaponBonus;

        Debug.Log("Attack dice: " + attackRoll);
        Debug.Log("Total attack: " + totalAttack);
    }

    public int GetMovementRoll()
    {
        return movementRoll;
    }

    public int GetAttackRoll()
    {
        return attackRoll;
    }

    public int GetTotalMovement()
    {
        return totalMovement;
    }

    public int GetTotalAttack()
    {
        return totalAttack;
    }

}

public enum TurnPhase
{
    RollMovement, Move, RollAttack, Attack
}
