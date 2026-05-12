using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public static System.Action<int> OnTurnStarted;
    public static System.Action<int> OnMovementRolled;
    public static System.Action<int> OnAttackRolled;
    public static System.Action<int> OnDamageDealt;

    public TurnPlayerController[] Players;
    public int CurrentPlayerIndex = 0;

    public TurnPhase currentPhase;

    public int MovesUsedthisTurn { get; private set; }
    public bool HasAttackedThisTurn { get; private set; }


     // Dice results
     private int movementRoll;
     private int attackRoll;

    // Dice + bonuses
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

    public void BeginGame()
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

        MovesUsedthisTurn = 0;
        HasAttackedThisTurn = false;

        //Start first phase
        currentPhase = TurnPhase.RollMovement;
        ShipInfo currentShipInfo = Players[CurrentPlayerIndex].GetComponent<ShipInfo>();
        if (currentShipInfo != null)
            currentShipInfo.ApplyStartTurnEffects();

        OnTurnStarted?.Invoke(Players[CurrentPlayerIndex].playerID);

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
                EndTurn(); // end turn, next player
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
        OnMovementRolled?.Invoke(totalMovement);

        Debug.Log("movement dice: " + movementRoll);
        Debug.Log("total movement: " + totalMovement);
    }

    public void SetAttackRoll(int value)
    {
        attackRoll = value;

        int weaponBonus = GetCurrentPlayer().weaponBonus;
        totalAttack = attackRoll + weaponBonus;
        OnAttackRolled?.Invoke(totalAttack);

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

    public void StartMovePhase()
    {
        currentPhase = TurnPhase.Move;
        Debug.Log("Move phase has started");
    }

    public void FinishMoveAction()
    {
        MovesUsedthisTurn++;

        if (MovesUsedthisTurn >= 2)
        {
            EndTurn();
            return;
        }

        currentPhase = TurnPhase.RollAttack;
        Debug.Log("Choose second action: press M for move or A for attack");
    }

    public void StartAttackPhase()
    {
        currentPhase = TurnPhase.Attack;
        Debug.Log("Attack phase has started");
    }

    public void FinishAttackAction()
    {
        HasAttackedThisTurn = true;
        EndTurn();
    }

    public static void BroadcastMovementRolled(int totalMovement)
    {
        OnMovementRolled?.Invoke(totalMovement);
    }

    public static void BroadcastAttackRolled(int totalAttack)
    {
        OnAttackRolled?.Invoke(totalAttack);
    }

    public static void BroadcastDamageDealt(int damage)
    {
        OnDamageDealt?.Invoke(damage);
    }

}

public enum TurnPhase
{
    RollMovement, Move, RollAttack, Attack
}
