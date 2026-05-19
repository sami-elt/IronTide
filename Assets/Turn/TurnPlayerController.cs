using Unity.VisualScripting;
using UnityEngine;

public class TurnPlayerController : MonoBehaviour
{


    public int playerID;
    private bool isMyTurn = false;
    public bool IsMyTurn { get => isMyTurn; }

    [Header("card bonuses")]
    public int motorBonus;
    public int weaponBonus;
    public int armorBonus; //finns inget än

    [Header("references")]
    [SerializeField] private ShipMovement shipMovement;
    [SerializeField] private ShipWeapon shipWeapon;

    //public void SetMyTurn(bool value)
    //{
    //    isMyTurn = value;

    //    Debug.Log("Player" + playerID + " is my turn: " + isMyTurn);

    //    if (isMyTurn && shipMovement != null)
    //    {
    //        shipMovement.avaliableTileDistance = 0;
    //    }
    //}

    public void SetMyTurn(bool value)
    {
        isMyTurn = value;


        if (isMyTurn && shipMovement != null)
        {
            shipMovement.avaliableTileDistance = 0;

            // Visa rätt tärning direkt när det blir spelarens tur
            int sides = shipMovement.ship.shipInfo.GetEngineDice();
            //FindFirstObjectByType<DiceManager>().ActiveDice(sides);
        }
        else if (!isMyTurn)
        {
            // Göm tärningen när turen är slut
            FindFirstObjectByType<DiceManager>().HideAllDice();
        }
    }

    private void Awake()
    {


        if (shipMovement == null)
        {
            shipMovement = GetComponent<ShipMovement>();
        }

        if (shipWeapon == null)
        {
            shipWeapon = GetComponent<ShipWeapon>();
        }
    }

    void Update()
    {
       
        if (!isMyTurn)
        {
            return;
        }

        HandleMovePhaseAutoProgress();
        HandleAttackPhaseAutoProgress();

        if (Input.GetKeyDown(KeyCode.M))
        {
            OnMoveButtonClicked();
            //HandleMoveKey();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            OnAttackButtonClicked();
            //HandleAttackKey();
        }
    }

    public void OnMoveButtonClicked()
    {
        var phase = TurnManager.Instance.currentPhase;

        switch (phase)
        {
            case TurnPhase.RollMovement:
                StartMoveAction();
                break;

            case TurnPhase.Move:
                TryFinishMovePhase();
                break;

            case TurnPhase.RollAttack:
                OnAttackButtonClicked();
                break;
        }
    }

    public void RequestMoveAction()
    {
        if (!isMyTurn)
            return;

        HandleMoveKey();
    }

    public void RequestAttackAction()
    {
        if (!isMyTurn)
            return;

        HandleAttackKey();
    }

    public void RequestEndCurrentAction()
    {
        if (!isMyTurn || TurnManager.Instance == null)
            return;

        switch (TurnManager.Instance.currentPhase)
        {
            case TurnPhase.Move:
                TryFinishMovePhase();
                break;
            case TurnPhase.RollAttack:
            case TurnPhase.Attack:
                TurnManager.Instance.FinishAttackAction();
                break;
        }
    }

    public bool OnAttackButtonClicked()
    {
        if (TurnManager.Instance.currentPhase != TurnPhase.RollAttack)
            return false;

        if (shipWeapon == null)
        {
            Debug.Log("shipWeapon missing on player " + playerID);
            return false;
        }

        return TryStartAttackAction();
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
        //if (diceComponent == null)
        //{
        //    Debug.Log("diceComponent missing on player " + playerID);
        //    return;
        //}

        //int moveRoll = diceComponent.RollD6();
        //TurnManager.Instance.SetMovementRoll(moveRoll);

        //int totalMove = TurnManager.Instance.GetTotalMovement();

        if (shipMovement != null)
        {
            //shipMovement.avaliableTileDistance = totalMove;
            shipMovement.EnterMovePhase(true);
        }

        //Debug.Log("Player " + playerID + " rolled a " + moveRoll + " for movement.");
        //Debug.Log("Player " + playerID + " has a total movement of " + totalMove + ".");


        //kommenterar bort här sålänge med nya dice
        //TurnManager.Instance.NextPhase();
    }

    private void TryFinishMovePhase()
    {
        if (shipMovement == null)
        {
            Debug.Log("shipMovement missing on player " + playerID);
            TurnManager.Instance.FinishMoveAction();
            return;
        }

        if (shipMovement.Moving)
        {
            shipMovement.SkipMove();
        }

        //if (shipMovement.avaliableTileDistance <= 0)
        //{
            Debug.Log("Player " + playerID + " finished moving.");
            TurnManager.Instance.FinishMoveAction();
        //}
        //else
        //{
        //    Debug.Log("Player " + playerID + " still has " + shipMovement.avaliableTileDistance + " movement left.");
        //}
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

        if (shipMovement.Moving)
        {
            return;
        }

        Debug.Log("AutoProgress check — avaliableTileDistance: " + shipMovement.avaliableTileDistance);

        if (shipMovement.avaliableTileDistance <= 0)
        {
            Debug.Log("Player " + playerID + " finished moving.");
            TurnManager.Instance.FinishMoveAction();
        }
    }

    private void RollAttackPhase()
    {
        //if (diceComponent == null)
        //{
        //    Debug.Log("diceComponent missing on player " + playerID);
        //    return;
        //}

        //int attackRoll = diceComponent.RollD6();
        //TurnManager.Instance.SetAttackRoll(attackRoll);

        //Debug.Log("Player " + playerID + " rolled a " + attackRoll + " for attack.");
        //Debug.Log("Player " + playerID + " has a total attack of " + TurnManager.Instance.GetTotalAttack() + ".");

        TryStartAttackAction();
    }

    private void FinishAttackPhase()
    {
        // For now, just end the turn after rolling attack

        Debug.Log("Player " + playerID + " finished attacking.");
        TurnManager.Instance.NextPhase();
    }

    private void HandleMoveKey()
    {
        var phase = TurnManager.Instance.currentPhase;

        switch (phase)
        {
            case TurnPhase.RollMovement:
                StartMoveAction();
                break;

            case TurnPhase.Move:
                TryFinishMovePhase();
                break;

            case TurnPhase.RollAttack:
                StartMoveAction();
                break;
        }
    }

    private void HandleAttackKey()
    {
        if (TurnManager.Instance.currentPhase != TurnPhase.RollAttack)
        {
            return;
        }

        if (shipWeapon == null)
        {
            Debug.Log("shipWeapon missing on player " + playerID);
            return;
        }

        TryStartAttackAction();
    }

    private void StartMoveAction()
    {
        if (shipMovement == null)
        {
            Debug.Log("shipMovement missing on player " + playerID);
            return;
        }

        shipMovement.EnterMovePhase(true);

        //kommenterar bort sålänge med nya dice
        //TurnManager.Instance.StartMovePhase();
    }

    private bool TryStartAttackAction()
    {
        if (shipWeapon == null)
        {
            Debug.Log("shipWeapon missing on player " + playerID);
            return false;
        }

        shipWeapon.EnterAttackPhase();
        int targetCount = shipWeapon.ReachableTargetsDamageModifiers.Count;
        if (targetCount <= 0)
        {
            TurnManager.BroadcastTurnFeedback("No enemies in range. Press M to move instead.");
            return false;
        }

        TurnManager.Instance.StartAttackPhase();
        TurnManager.BroadcastTurnFeedback(targetCount == 1
            ? "Attack ready: 1 enemy in range."
            : "Attack ready: " + targetCount + " enemies in range.");
        return true;
    }

    private void HandleAttackPhaseAutoProgress()
    {
        if (TurnManager.Instance.currentPhase != TurnPhase.Attack)
        {
            return;
        }

        if (shipWeapon == null)
        {
            return;
        }

        if (shipWeapon.HasAttacked)
        {
            Debug.Log("Player " + playerID + " finished attacking.");
            TurnManager.Instance.FinishAttackAction();
        }

       
    }
}
