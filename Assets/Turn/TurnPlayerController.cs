using UnityEngine;

public class TurnPlayerController : MonoBehaviour
{
    public int playerID;
    private bool isMyTurn = false;

    [Header("card bonuses")]
    public int motorBonus;
    public int weaponBonus;
    public int armorBonus;

    [Header("references")]
    [SerializeField] private DiceComponent diceComponent;
    [SerializeField] private ShipMovement shipMovement;

    public void SetMyTurn(bool value)
    {
        isMyTurn = value;

        Debug.Log("Player" + playerID + " is my turn: " + isMyTurn);

        if (isMyTurn && shipMovement != null)
        {
            shipMovement.avaliableTileDistance = 0;
        }
    }

    private void Awake()
    {
        if (diceComponent == null)
        {
            diceComponent = GetComponent<DiceComponent>();
        }

        if (shipMovement == null)
        {
            shipMovement = GetComponent<ShipMovement>();
        }
    }

    void Update()
    {
        if (!isMyTurn)
        {
            return;
        }
        HandleMovePhaseAutoProgress();

        if (Input.GetKeyDown(KeyCode.M))
        {
            HandlePhase();
        }
    }

    private void HandlePhase()
    {
        var phase = TurnManager.Instance.currentPhase;

        switch (phase)
        {
            case TurnPhase.RollMovement:
                RollMovementPhase();
                break;

            case TurnPhase.Move:
                TryFinishMovePhase();
                break;

            case TurnPhase.RollAttack:
                RollAttackPhase();
                break;

            case TurnPhase.Attack:
                FinishAttackPhase();
                break;
        }
    }

    private void RollMovementPhase()
    {
        if (diceComponent == null)
        {
            Debug.Log("diceComponent missing on player " + playerID);
            return;
        }

        int moveRoll = diceComponent.RollD6();
        TurnManager.Instance.SetMovementRoll(moveRoll);

        int totalMove = TurnManager.Instance.GetTotalMovement();

        if (shipMovement != null)
        {
            shipMovement.avaliableTileDistance = totalMove;
        }

        Debug.Log("Player " + playerID + " rolled a " + moveRoll + " for movement.");
        Debug.Log("Player " + playerID + " has a total movement of " + totalMove + ".");

        TurnManager.Instance.NextPhase();
    }

    private void TryFinishMovePhase()
    {
        if (shipMovement == null)
        {
            Debug.Log("shipMovement missing on player " + playerID);
            TurnManager.Instance.NextPhase();
            return;
        }

        if (shipMovement.avaliableTileDistance <= 0)
        {
            Debug.Log("Player " + playerID + " finished moving.");
            TurnManager.Instance.NextPhase();
        }
        else
        {
            Debug.Log("Player " + playerID + " still has " + shipMovement.avaliableTileDistance + " movement left.");
        }
    }

    private void HandleMovePhaseAutoProgress()
    {
        if (TurnManager.Instance.currentPhase != TurnPhase.Move)
        {
            return;
        }

        if (shipMovement == null)
        {
            return;
        }

        if (shipMovement.avaliableTileDistance <= 0)
        {
            Debug.Log("Player " + playerID + " finished moving.");
            TurnManager.Instance.NextPhase();
        }
    }

    private void RollAttackPhase()
    {
        if (diceComponent == null)
        {
            Debug.Log("diceComponent missing on player " + playerID);
            return;
        }

        int attackRoll = diceComponent.RollD6();
        TurnManager.Instance.SetAttackRoll(attackRoll);

        Debug.Log("Player " + playerID + " rolled a " + attackRoll + " for attack.");
        Debug.Log("Player " + playerID + " has a total attack of " + TurnManager.Instance.GetTotalAttack() + ".");

        TurnManager.Instance.NextPhase();
    }

    private void FinishAttackPhase()
    {
        // For now, just end the turn after rolling attack
        Debug.Log("Player " + playerID + " finished attacking.");
        TurnManager.Instance.NextPhase();
    }
}
