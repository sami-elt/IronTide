using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class TurnPlayerController : MonoBehaviour
{
    public int playerID;
    private bool isMyTurn = false;
    public bool IsMyTurn { get => isMyTurn; }
    public bool IsResolvingAttackResult => finishAttackAfterDelayRoutine != null;
    public bool CanReceiveTurnInput => isMyTurn && !IsResolvingAttackResult && IsCameraReadyForTurnInput();

    [Header("card bonuses")]
    public int motorBonus;
    public int weaponBonus;
    public int armorBonus; //finns inget än

    [Header("references")]
    [SerializeField] private DiceComponent diceComponent;
    [SerializeField] private ShipMovement shipMovement;
    [SerializeField] private ShipWeapon shipWeapon;
    [SerializeField] private float attackResultPause = 1.26f;

    private Coroutine finishAttackAfterDelayRoutine;

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

        if (shipWeapon == null)
        {
            shipWeapon = GetComponent<ShipWeapon>();
        }
    }

    private void OnDisable()
    {
        if (finishAttackAfterDelayRoutine != null)
        {
            StopCoroutine(finishAttackAfterDelayRoutine);
            finishAttackAfterDelayRoutine = null;
        }
    }

    void Update()
    {
       
        if (!CanReceiveTurnInput)
        {
            return;
        }
        HandleMovePhaseAutoProgress();
        HandleAttackPhaseAutoProgress();

        if (Input.GetKeyDown(KeyCode.M))
        {
            HandleMoveKey();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            HandleAttackKey();
        }
    }

    public void RequestMoveAction()
    {
        if (!CanReceiveTurnInput)
            return;

        HandleMoveKey();
    }

    public void OnMoveButtonClicked()
    {
        RequestMoveAction();
    }

    public void RequestAttackAction()
    {
        if (!CanReceiveTurnInput)
            return;

        HandleAttackKey();
    }

    public void OnAttackButtonClicked()
    {
        RequestAttackAction();
    }

    public void RequestEndCurrentAction()
    {
        if (!CanReceiveTurnInput || TurnManager.Instance == null)
            return;

        switch (TurnManager.Instance.currentPhase)
        {
            case TurnPhase.RollMovement:
                TurnManager.Instance.FinishAttackAction();
                break;
            case TurnPhase.Move:
                TryFinishMovePhase();
                break;
            case TurnPhase.RollAttack:
            case TurnPhase.Attack:
                TurnManager.Instance.FinishAttackAction();
                break;
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

        TurnManager.Instance.NextPhase();
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
        if (TurnManager.Instance.currentPhase != TurnPhase.RollAttack &&
            TurnManager.Instance.currentPhase != TurnPhase.RollMovement)
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
        TurnManager.Instance.StartMovePhase();
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
            Debug.Log("Player " + playerID + " cannot attack because no enemies are in range.");
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
            if (finishAttackAfterDelayRoutine == null)
                finishAttackAfterDelayRoutine = StartCoroutine(FinishAttackAfterResultPause());
        }

       
    }

    private IEnumerator FinishAttackAfterResultPause()
    {
        Debug.Log("Player " + playerID + " finished attacking. Showing result before next turn.");

        yield return new WaitForSeconds(Mathf.Max(0f, attackResultPause));

        finishAttackAfterDelayRoutine = null;

        if (!isMyTurn || TurnManager.Instance == null || TurnManager.Instance.currentPhase != TurnPhase.Attack)
            yield break;

        TurnManager.Instance.FinishAttackAction();
    }

    private bool IsCameraReadyForTurnInput()
    {
        Camera mainCamera = Camera.main;
        CameraController cameraController = mainCamera != null
            ? mainCamera.GetComponent<CameraController>()
            : null;

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();

        return cameraController == null || cameraController.IsReadyForTurnInput(transform);
    }
}
