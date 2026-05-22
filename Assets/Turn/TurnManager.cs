using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public static System.Action<int> OnTurnStarted;
    public static System.Action<int> OnMovementRolled;
    public static System.Action<int> OnAttackRolled;
    public static System.Action<int> OnDamageDealt;
    public static System.Action<string> OnTurnFeedback;
    public static System.Action<int, int, int, int> OnAttackResolved;
    public static System.Action<int, int> OnAttackPrepared;

    public TurnPlayerController[] Players;
    public int CurrentPlayerIndex = 0;

    public TurnPhase currentPhase;

    public int MovesUsedthisTurn { get; private set; }
    public bool HasAttackedThisTurn { get; private set; }

    private readonly Dictionary<int, HashSet<int>> attackedPlayersThisRound = new Dictionary<int, HashSet<int>>();

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
        //ResolvePlayers();
        //StartTurn();
    }

    public void BeginGame()
    {
        attackedPlayersThisRound.Clear();
        StartTurn();
    }

    private void StartTurn()
    {

        ResolvePlayers();
        PrepareAssignedPlayers();
        DiceVisualManager.HideActiveRoll();

        if (Players == null || Players.Length == 0)
        {
            Debug.LogWarning("TurnManager could not start because no players are assigned.");
            return;
        }

        CurrentPlayerIndex = GetNextPlayablePlayerIndex(CurrentPlayerIndex);
        if (CurrentPlayerIndex < 0)
        {
            Debug.LogWarning("TurnManager could not start because no active players are available.");
            return;
        }

        Debug.Log(
    "Before turn active: " +
    Players[CurrentPlayerIndex].gameObject.activeSelf
);

        // Reset all players
        for (int i = 0; i < Players.Length; i++)
        {
            if (Players[i] != null)
                Players[i].SetMyTurn(false);
        }   
        //Active player
        if (!Players[CurrentPlayerIndex].gameObject.activeSelf)
            Players[CurrentPlayerIndex].gameObject.SetActive(true);

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
        if (Players != null && CurrentPlayerIndex >= 0 && CurrentPlayerIndex < Players.Length && Players[CurrentPlayerIndex] != null)
            Players[CurrentPlayerIndex].SetMyTurn(false);

        CurrentPlayerIndex++;

        if (Players == null || Players.Length == 0)
            return;

        if (CurrentPlayerIndex >= Players.Length)
        {
            CurrentPlayerIndex = 0;
        }

        StartTurn();
    }

    public TurnPlayerController GetCurrentPlayer()
    {
        if (Players == null || Players.Length == 0 || CurrentPlayerIndex < 0 || CurrentPlayerIndex >= Players.Length)
            return null;

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

    public void RecordShipAttack(ShipInfo attacker, ShipInfo target)
    {
        int attackerId = GetShipPlayerId(attacker);
        int targetId = GetShipPlayerId(target);
        if (attackerId < 0 || targetId < 0 || attackerId == targetId)
            return;

        if (!attackedPlayersThisRound.TryGetValue(attackerId, out var attackedTargets))
        {
            attackedTargets = new HashSet<int>();
            attackedPlayersThisRound[attackerId] = attackedTargets;
        }

        attackedTargets.Add(targetId);
    }

    public bool HasShipAttackedTargetThisRound(ShipInfo attacker, ShipInfo target)
    {
        return HasPlayerAttackedTargetThisRound(GetShipPlayerId(attacker), GetShipPlayerId(target));
    }

    public bool HasPlayerAttackedTargetThisRound(int attackerId, int targetId)
    {
        return attackerId >= 0 &&
            targetId >= 0 &&
            attackedPlayersThisRound.TryGetValue(attackerId, out var attackedTargets) &&
            attackedTargets.Contains(targetId);
    }

    private int GetShipPlayerId(ShipInfo info)
    {
        if (info == null)
            return -1;

        TurnPlayerController controller = info.GetComponent<TurnPlayerController>();
        return controller != null ? controller.playerID : -1;
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

    public static void BroadcastTurnFeedback(string message)
    {
        OnTurnFeedback?.Invoke(message);
    }

    public static void BroadcastAttackResolved(int diceTotal, int bonusTotal, int damageReduction, int damageDealt)
    {
        OnAttackResolved?.Invoke(diceTotal, bonusTotal, damageReduction, damageDealt);
    }

    public static void BroadcastAttackPrepared(int diceTotal, int bonusTotal)
    {
        OnAttackPrepared?.Invoke(diceTotal, bonusTotal);
    }

    private void ResolvePlayers()
    {
        if (Players != null && Players.Length > 0)
            return;

        TurnPlayerController[] foundPlayers = Resources.FindObjectsOfTypeAll<TurnPlayerController>();
        var scenePlayers = new List<TurnPlayerController>();
        foreach (TurnPlayerController player in foundPlayers)
        {
            if (player == null || !player.gameObject.scene.IsValid())
                continue;

            scenePlayers.Add(player);
        }

        scenePlayers.Sort((a, b) => a.playerID.CompareTo(b.playerID));
        Players = scenePlayers.ToArray();
    }

    //private void ResolvePlayers()
    //{
    //    Debug.Log("Resolving players...");

    //    TurnPlayerController[] foundPlayers =
    //        FindObjectsByType<TurnPlayerController>(
    //            FindObjectsSortMode.None);

    //    foreach (var p in foundPlayers)
    //    {
    //        Debug.Log("FOUND PLAYER: " + p.name +
    //                  " active: " + p.gameObject.activeSelf);
    //    }

    //    Players = foundPlayers;
    //}

    private int GetNextPlayablePlayerIndex(int startIndex)
    {
        if (Players == null || Players.Length == 0)
            return -1;

        int index = Mathf.Clamp(startIndex, 0, Players.Length - 1);
        int fallbackIndex = -1;

        for (int i = 0; i < Players.Length; i++)
        {
            int candidateIndex = (index + i) % Players.Length;
            TurnPlayerController candidate = Players[candidateIndex];
            if (candidate == null)
                continue;

            if (fallbackIndex < 0)
                fallbackIndex = candidateIndex;

            if (!candidate.gameObject.activeInHierarchy)
                continue;

            ShipInfo info = candidate.GetComponent<ShipInfo>();
            if (info != null && info.Sunk)
                continue;

            return candidateIndex;
        }

        return fallbackIndex;
    }

    private void PrepareAssignedPlayers()
    {
        if (Players == null)
            return;

        for (int i = 0; i < Players.Length; i++)
        {
            TurnPlayerController player = Players[i];
            if (player == null)
                continue;

            if (!player.gameObject.activeSelf)
                player.gameObject.SetActive(true);

            ShipInfo info = player.GetComponent<ShipInfo>();
            if (info != null && info.Sunk && HasAnyModuleAssigned(info))
                info.ResetValues();
        }
    }

    private bool HasAnyModuleAssigned(ShipInfo info)
    {
        return info != null &&
            (info.WeaponModule != null || info.ArmorModule != null || info.EngineModule != null);
    }

}

public enum TurnPhase
{
    RollMovement, Move, RollAttack, Attack
}
