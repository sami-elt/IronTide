using System.Collections.Generic;
using UnityEngine;

public class GameManagerTest : MonoBehaviour
{
    public GenerateMap map;
    public GameObject playerPrefab;

    List<GameObject> players = new List<GameObject>();

    public void OnMapReady()
    {
        SpawnPlayers();
    }

    void SpawnPlayers()
    {
<<<<<<< Updated upstream
        foreach (var pos in map.spawnWorldPositions)
=======
        if (IronTideGameState.Players.Count == 0)
            IronTideGameState.EnsurePlayers(map != null ? map.playerCount : 2);

        IReadOnlyList<IronTidePlayerState> playerData =
            IronTideGameState.Players;

        TurnPlayerController[] spawnedPlayers =
            new TurnPlayerController[playerData.Count];

        for (int i = 0; i < playerData.Count; i++)
>>>>>>> Stashed changes
        {
            GameObject p = Instantiate(playerPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
            players.Add(p);
        }
<<<<<<< Updated upstream
=======

        TurnManager.Instance.Players = spawnedPlayers;

        TestDay1PlayUI ui = FindFirstObjectByType<TestDay1PlayUI>();

        if (ui != null)
        {
            ui.ships = new List<Ship>();
            ui.ships.Clear();

            foreach (TurnPlayerController player in spawnedPlayers)
            {
                if (player == null)
                    continue;

                Ship ship = player.GetComponent<Ship>();

                if (ship != null)
                {
                    ui.ships.Add(ship);
                }
            }
            ui.RebuildUI();
            Debug.Log("UI SHIPS COUNT: " + ui.ships.Count);

            
        }

        TurnManager.Instance.BeginGame();

        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
            cameraController.FocusCurrentTurnPlayer(true);

        Debug.Log("Spawn klart");
        

       
>>>>>>> Stashed changes
    }
}
