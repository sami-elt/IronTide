using System.Collections.Generic;
using UnityEngine;

public class GameManagerTest : MonoBehaviour
{
    public GenerateMap map;

    public GameObject playerPrefab;
    public IronTide.BasicCards.IronTideModuleCardLibrary moduleLibrary;

    List<GameObject> players = new List<GameObject>();

    public void OnMapReady()
    {
        SpawnPlayers();
    }

    void SpawnPlayers()
    {
        if (IronTideGameState.Players.Count == 0)
            IronTideGameState.EnsurePlayers(map != null ? map.playerCount : 2);

        IReadOnlyList<IronTidePlayerState> playerData =
            IronTideGameState.Players;

        TurnPlayerController[] spawnedPlayers =
            new TurnPlayerController[playerData.Count];

        for (int i = 0; i < playerData.Count; i++)
        {
            Vector3 pos = map.spawnWorldPositions[i];

            GameObject p = Instantiate(
                playerPrefab,
                pos + Vector3.up * 0.5f,
                Quaternion.identity
            );

            // APPLY LOADOUTS
            Ship ship = p.GetComponent<Ship>();

            if (ship != null)
            {
                IronTideGameState.ApplyLoadoutToShip(
                    ship,
                    playerData[i],
                    moduleLibrary
                );
                ship.shipInfo.ResetValues();
            }

            p.name = playerData[i].DisplayName;

            Renderer rend =
                p.GetComponentInChildren<Renderer>();

            if (rend != null)
            {
                rend.material.color =
                    playerData[i].PlayerColor;
            }

            TurnPlayerController controller =
                p.GetComponent<TurnPlayerController>();

            if (controller != null)
            {
                controller.playerID =
                    playerData[i].PlayerId;

                spawnedPlayers[i] = controller;
            }

            players.Add(p);
        }

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
        

       
    }
}
