using UnityEngine;
using UnityEngine.UI;

public class TurnActionUI : MonoBehaviour
{
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button endTurnButton;

    private DiceManager diceManager;

    private void Start()
    {
        diceManager = FindFirstObjectByType<DiceManager>();

        moveButton.onClick.AddListener(OnMoveClicked);
        attackButton.onClick.AddListener(OnAttackClicked);
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    private void Update()
    {
        TurnPlayerController activePlayer = GetActivePlayer();

        if (activePlayer == null)
        {
            SetAllButtons(false);
            return;
        }

        TurnPhase phase = TurnManager.Instance.currentPhase;
        int movesUsed = TurnManager.Instance.MovesUsedthisTurn;

        moveButton.interactable = (phase == TurnPhase.RollMovement || phase == TurnPhase.RollAttack)
                                   && movesUsed < 2;

        //uppdatera så man kan attackera direkt
        attackButton.interactable = (phase == TurnPhase.RollAttack || phase == TurnPhase.RollMovement)
                                    && !TurnManager.Instance.HasAttackedThisTurn;

        //välja att kunna avsluta sin runda direkt
        endTurnButton.interactable = phase == TurnPhase.RollMovement
                             || phase == TurnPhase.RollAttack
                             || phase == TurnPhase.Move;

        //endTurnButton.interactable = phase == TurnPhase.RollMovement
        //                             || phase == TurnPhase.RollAttack;
    }



    private bool isProcessing = false;


    private void OnMoveClicked()
    {
        if (isProcessing) return;
        isProcessing = true;

        TurnPlayerController activePlayer = GetActivePlayer();
        if (activePlayer == null) { isProcessing = false; return; }

        ShipMovement movement = activePlayer.GetComponent<ShipMovement>();

        // Visa rätt tärning när spelaren väljer att röra sig
        int sides = movement.ship.shipInfo.GetEngineDice();
        diceManager.ActiveDice(sides);

        if (!movement.isWaitingForDice)
            activePlayer.OnMoveButtonClicked();

        diceManager.RollForMovement(movement);

        Invoke(nameof(ResetProcessing), 0.5f);
    }
    //private void OnAttackClicked()
    //{
    //    if (isProcessing) return;
    //    isProcessing = true;

    //    TurnPlayerController activePlayer = GetActivePlayer();
    //    if (activePlayer == null) { isProcessing = false; return; }

    //    // Gå direkt till attack utan att räkna som rörelse
    //    TurnManager.Instance.GoToAttack();
    //    activePlayer.OnAttackButtonClicked();

    //    Invoke(nameof(ResetProcessing), 0.5f);
    //}

    private void OnAttackClicked()
    {
        if (isProcessing) return;
        isProcessing = true;

        TurnPlayerController activePlayer = GetActivePlayer();
        if (activePlayer == null) { isProcessing = false; return; }

        TurnManager.Instance.GoToAttack();
        activePlayer.OnAttackButtonClicked();

        Invoke(nameof(ResetProcessing), 0.5f);
    }
    private void OnEndTurnClicked()
    {
        if (isProcessing) return;
        isProcessing = true;

        TurnPhase phase = TurnManager.Instance.currentPhase;

        if (phase == TurnPhase.Move)
        {
            // Avsluta rörelsen, gå till RollAttack
            TurnManager.Instance.FinishMoveAction();
        }
        else if (phase == TurnPhase.RollAttack)
        {
            // Avsluta turen helt
            TurnManager.Instance.FinishAttackAction();
        }
        else if (phase == TurnPhase.RollMovement)
        {
            // Skippa rörelse, gå till attack
            TurnManager.Instance.GoToAttack();
        }

        Invoke(nameof(ResetProcessing), 0.5f);
    }


    private void ResetProcessing()
    {
        isProcessing = false;
    }

    private void SetAllButtons(bool interactable)
    {
        moveButton.interactable = interactable;
        attackButton.interactable = interactable;
        endTurnButton.interactable = interactable;
    }

    private TurnPlayerController GetActivePlayer()
    {
        TurnPlayerController[] allPlayers = FindObjectsByType<TurnPlayerController>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
            if (player.IsMyTurn) return player;
        return null;
    }
}