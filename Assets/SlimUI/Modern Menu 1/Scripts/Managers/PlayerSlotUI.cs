using UnityEngine;

public class PlayerSetupUI : MonoBehaviour
{
    public GameObject[] playerSlots;

    public void ShowPlayers(int count)
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            playerSlots[i].SetActive(i < count);
        }
    }
}