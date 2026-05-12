using System.Collections.Generic;
using UnityEngine;

public class GameManagerT : MonoBehaviour
{
    public static GameManagerT Instance;

    public List<PlayerData> players;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetPlayers(List<PlayerData> playerList)
    {
        players = playerList;
    }
}