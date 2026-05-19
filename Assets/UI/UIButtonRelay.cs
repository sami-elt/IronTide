using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public void ClickRollDice()
    {
        // Leta upp alla skepp ute på havet
        TurnPlayerController[] allPlayers = FindObjectsByType<TurnPlayerController>(FindObjectsSortMode.None);

        // Hitta det skepp vars tur det är
        foreach (var player in allPlayers)
        {
            if (player.IsMyTurn)
            {
                player.OnMoveButtonClicked();
                return; 
            }
        }
    }
}
