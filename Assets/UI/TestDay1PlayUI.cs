using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class TestDay1PlayUI : MonoBehaviour
{
    public TextMeshProUGUI turnIndicator;
    public TextMeshProUGUI moveIndicator;
    public TextMeshProUGUI rangeIndicator;
    public TextMeshProUGUI damageIndicator;

    public TextMeshProUGUI winIndicator;

    public TextMeshProUGUI phaseIndicator;

    public List<TextMeshProUGUI> healthIndicators;

    public List<Ship> ships;

    private bool gameOver = false;
    private int winnerNumber;

    void Update()
    {
        foreach (Ship ship in ships)
        {
            int playerId = ship.turnPlayerController.playerID;

            if (ship.shipInfo.Sunk && !gameOver)
            {
                gameOver = true;
            }
            else if (!ship.shipInfo.Sunk && gameOver && winnerNumber == 0)
            {
                winnerNumber = ship.turnPlayerController.playerID + 1;
                winIndicator.SetText($"Player {winnerNumber} won!");
                winIndicator.gameObject.SetActive(true);
            }

            if (gameOver)
            {
                continue;
            }

            healthIndicators[playerId].SetText($"Player {playerId + 1} HP: {ship.shipInfo.Health}/{ship.shipInfo.MaxHealth}");

            if (ship.turnPlayerController.IsMyTurn)
            {
                turnIndicator.SetText($"Player {playerId + 1}'s turn");
                moveIndicator.SetText($"{ship.shipMovement.avaliableTileDistance} steps avaliable");
                rangeIndicator.SetText($"You have a range of {ship.shipInfo.GetWeaponRange()} tiles");
                damageIndicator.SetText($"Your weapon deals a D6 of damage");
            }

            switch (TurnManager.Instance.currentPhase)
            {
                case TurnPhase.RollMovement:
                    phaseIndicator.SetText($"Press m to roll your move steps");
                    break;

                case TurnPhase.Move:
                    phaseIndicator.SetText($"Currently in move phase, you may move the amount of steps shown or less");
                    break;

                case TurnPhase.RollAttack:
                    phaseIndicator.SetText($"Press m to continue to attack phase");
                    break;

                case TurnPhase.Attack:
                    phaseIndicator.SetText($"Currently in attack phase, you may attack an enemy within your range along the tiles");
                    break;
            }
        }
    }
}
