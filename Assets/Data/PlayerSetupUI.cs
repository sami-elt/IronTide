using System.Collections.Generic;
using UnityEngine;

public class PlayerSetupUI : MonoBehaviour
{
    public PlayerSlotUI[] slots;
    public PlayerSlotUI currentSlot;
    public GameObject colorPickerPanel;
    public GameObject iconPickerPanel;

    public void ShowPlayers(int count)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].gameObject.SetActive(i < count);
        }
    }

    public void SetCurrentSlot(PlayerSlotUI slot)
    {
        currentSlot = slot;
    }


    public void SelectRed()
    {
        currentSlot.SetColor(Color.red);
        colorPickerPanel.SetActive(false);
    }

    public void SelectBlue()
    {
        currentSlot.SetColor(Color.blue);
        colorPickerPanel.SetActive(false);
    }

    public void SelectGreen()
    {
        currentSlot.SetColor(Color.green);
        colorPickerPanel.SetActive(false);
    }

    public void SelectYellow()
    {
        currentSlot.SetColor(Color.yellow);
        colorPickerPanel.SetActive(false);
    }
    public void SelectGrey()
    {
        currentSlot.SetColor(Color.grey);
        colorPickerPanel.SetActive(false);
    }

    public void SelectIcon(Sprite icon)
    {
        currentSlot.SetIcon(icon);
        iconPickerPanel.SetActive(false);
    }


    public List<PlayerData> GetPlayers()
    {
        List<PlayerData> players = new List<PlayerData>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].gameObject.activeSelf) continue;

            players.Add(slots[i].GetData());
        }

        return players;
    }

    public void TestPlayersData()
    {
        Debug.Log("BUTTON CLICK WORKS");
        List<PlayerData> players = GetPlayers();

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null) continue;

            Debug.Log(
                "Player: " + players[i].playerName +
                " | Color: " + players[i].playerColor +
                " | Icon: " + (players[i].icon != null ? players[i].icon.name : "NULL")
            );
        }
    }
    public void TestClick()
    {
        Debug.Log("BUTTON CLICK WORKS");
    }
}