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
        foreach (var pos in map.spawnWorldPositions)
        {
            GameObject p = Instantiate(playerPrefab, pos + Vector3.up * 0.5f, Quaternion.identity);
            players.Add(p);
        }
    }
}