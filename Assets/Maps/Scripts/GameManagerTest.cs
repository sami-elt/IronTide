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
        List<PlayerData> playerData = GameManagerT.Instance.players;

        for (int i = 0; i < playerData.Count; i++)
        {
            Vector3 pos = map.spawnWorldPositions[i];

            GameObject p = Instantiate(
                playerPrefab,
                pos + Vector3.up * 0.5f,
                Quaternion.identity
            );
            Renderer rend = p.GetComponentInChildren<Renderer>();

            if (rend != null)
            {
                rend.material.color = playerData[i].playerColor;
            }

            players.Add(p);

            // OPTIONAL
            p.name = playerData[i].playerName;
        }
    }
}