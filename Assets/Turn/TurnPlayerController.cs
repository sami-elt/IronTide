using System.Collections;
using UnityEngine;

public class TurnPlayerController : MonoBehaviour
{
    public int playerID;
    private bool isMyTurn = false;
    public bool IsMyTurn { get => isMyTurn; }
    public bool IsResolvingAttackResult => finishAttackAfterDelayRoutine != null;
    public bool CanReceiveTurnInput => isMyTurn && !IsSunk() && !IsResolvingAttackResult && IsCameraReadyForTurnInput();

    [Header("card bonuses")]
    public int motorBonus;
    public int weaponBonus;
    public int armorBonus;

    [Header("references")]
    [SerializeField] private ShipMovement shipMovement;
    [SerializeField] private ShipWeapon shipWeapon;
    [SerializeField] private float attackResultPause = 1.26f;

    private Coroutine finishAttackAfterDelayRoutine;

    public void SetMyTurn(bool value)
    {
        if (value && IsSunk())
        {
            isMyTurn = false;
            return;
        }

        isMyTurn = value;

        Debug.Log("Player" + playerID + " is my turn: " + isMyTurn);

        if (isMyTurn && shipMovement != null)
        {
            shipMovement.avaliableTileDistance = 0;
        }
    }

    private void Awake()
    {
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();

        if (shipWeapon == null)
            shipWeapon = GetComponent<ShipWeapon>();
    }

    private void OnDisable()
    {
        isMyTurn = false;

        if (finishAttackAfterDelayRoutine != null)
        {
            StopCoroutine(finishAttackAfterDelayRoutine);
            finishAttackAfterDelayRoutine = null;
        }
    }

    void Update()
    {
        if (!CanReceiveTurnInput)
            return;

        HandleMovePhaseAutoProgress();
        HandleAttackPhaseAutoProgress();

        if (Input.GetKeyDown(KeyCode.M))
            HandleMoveKey();

        if (Input.GetKeyDown(KeyCode.A))
            HandleAttackKey();
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

    private void RollMovementPhase()
    {
        if (shipMovement != null)
            shipMovement.EnterMovePhase(true);

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
            shipMovement.SkipMove();

        Debug.Log("Player " + playerID + " finished moving.");
        TurnManager.Instance.FinishMoveAction();
    }

    private void HandleMovePhaseAutoProgress()
    {
        if (TurnManager.Instance.currentPhase != TurnPhase.Move)
            return;

        if (shipMovement == null || shipMovement.Moving)
            return;

        if (shipMovement.avaliableTileDistance <= 0)
        {
            Debug.Log("Player " + playerID + " finished moving.");
            TurnManager.Instance.FinishMoveAction();
        }
    }

    private void RollAttackPhase()
    {
        TryStartAttackAction();
    }

    private void FinishAttackPhase()
    {
        Debug.Log("Player " + playerID + " finished attacking.");
        TurnManager.Instance.NextPhase();
    }

    private void HandleMoveKey()
    {
        switch (TurnManager.Instance.currentPhase)
        {
            case TurnPhase.RollMovement:
            case TurnPhase.RollAttack:
                StartMoveAction();
                break;

            case TurnPhase.Move:
                TryFinishMovePhase();
                break;
        }
    }

    private void HandleAttackKey()
    {
        if (TurnManager.Instance.currentPhase != TurnPhase.RollAttack &&
            TurnManager.Instance.currentPhase != TurnPhase.RollMovement)
            return;

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
            ? "Choose a target, then roll attack. 1 enemy in range."
            : "Choose a target, then roll attack. " + targetCount + " enemies in range.");
        return true;
    }

    private void HandleAttackPhaseAutoProgress()
    {
        if (TurnManager.Instance.currentPhase != TurnPhase.Attack)
            return;

        if (shipWeapon == null)
            return;

        if (shipWeapon.HasAttacked && finishAttackAfterDelayRoutine == null)
            finishAttackAfterDelayRoutine = StartCoroutine(FinishAttackAfterResultPause());
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

    private bool IsSunk()
    {
        ShipInfo info = GetComponent<ShipInfo>();
        return info != null && info.Sunk;
    }
}