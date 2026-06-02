using UnityEngine;
using UnityEngine.UI;

public class TurnActionUI : MonoBehaviour
{
    [SerializeField] private Button moveButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button endTurnButton;

    private void Start()
    {
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

        attackButton.interactable = (phase == TurnPhase.RollAttack || phase == TurnPhase.RollMovement)
                                    && !TurnManager.Instance.HasAttackedThisTurn;

        endTurnButton.interactable = phase == TurnPhase.RollMovement
                             || phase == TurnPhase.RollAttack
                             || phase == TurnPhase.Move;
    }

    private bool isProcessing = false;

    private void OnMoveClicked()
    {
        if (isProcessing) return;
        isProcessing = true;

        TurnPlayerController activePlayer = GetActivePlayer();
        if (activePlayer == null) { isProcessing = false; return; }

        activePlayer.OnMoveButtonClicked();

        Invoke(nameof(ResetProcessing), 0.5f);
    }

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
            TurnManager.Instance.FinishMoveAction();
        }
        else if (phase == TurnPhase.RollAttack)
        {
            TurnManager.Instance.FinishAttackAction();
        }
        else if (phase == TurnPhase.RollMovement)
        {
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
        foreach (TurnPlayerController player in allPlayers)
            if (player.IsMyTurn) return player;
        return null;
    }
}