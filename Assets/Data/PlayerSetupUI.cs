using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerSetupUI : MonoBehaviour
{
    public PlayerSlotUI[] slots;
    public PlayerSlotUI currentSlot;
    public GameObject colorPickerPanel;

    private static readonly Color[] PlayerPalette =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.grey
    };

    public void ShowPlayers(int count)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool active = i < count;
            slots[i].gameObject.SetActive(active);
            if (active)
                slots[i].SetColorSilently(GetDefaultColor(i));
        }
    }

    public void SetCurrentSlot(PlayerSlotUI slot)
    {
        currentSlot = slot;
    }


    public void SelectRed()
    {
        TrySelectColor(Color.red);
    }

    public void SelectBlue()
    {
        TrySelectColor(Color.blue);
    }

    public void SelectGreen()
    {
        TrySelectColor(Color.green);
    }

    public void SelectYellow()
    {
        TrySelectColor(Color.yellow);
    }
    public void SelectGrey()
    {
        TrySelectColor(Color.grey);
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
                " | Color: " + players[i].playerColor
            );
        }
    }
  
    public void StartGame()
    {
        EnsureUniqueActiveColors();
        List<PlayerData> players = GetPlayers();
        if (GameManagerT.Instance != null)
            GameManagerT.Instance.SetPlayers(players);

        IronTideGameState.ResetAll();
        IronTideGameState.ConfigurePlayers(players);
        SceneManager.LoadScene(IronTideGameState.CombatSceneName);
    }

    private void TrySelectColor(Color color)
    {
        if (currentSlot == null)
            return;

        if (IsColorUsedByAnotherActiveSlot(color, currentSlot))
        {
            Debug.LogWarning("Color already selected by another player.");
            if (colorPickerPanel != null)
                colorPickerPanel.SetActive(false);
            return;
        }

        currentSlot.SetColor(color);
        if (colorPickerPanel != null)
            colorPickerPanel.SetActive(false);
    }

    private void EnsureUniqueActiveColors()
    {
        if (slots == null)
            return;

        var usedColors = new List<Color>();
        for (int i = 0; i < slots.Length; i++)
        {
            PlayerSlotUI slot = slots[i];
            if (slot == null || !slot.IsActive)
                continue;

            Color selectedColor = FindPaletteColor(slot.CurrentColor, out bool matchesPalette)
                ? slot.CurrentColor
                : GetDefaultColor(i);

            if (!matchesPalette || ContainsColor(usedColors, selectedColor))
                selectedColor = GetFirstAvailableColor(usedColors);

            slot.SetColorSilently(selectedColor);
            usedColors.Add(selectedColor);
        }
    }

    private bool IsColorUsedByAnotherActiveSlot(Color color, PlayerSlotUI owner)
    {
        if (slots == null)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            PlayerSlotUI slot = slots[i];
            if (slot == null || slot == owner || !slot.IsActive)
                continue;

            if (ColorsMatch(slot.CurrentColor, color))
                return true;
        }

        return false;
    }

    private static bool FindPaletteColor(Color color, out bool matchesPalette)
    {
        matchesPalette = false;
        for (int i = 0; i < PlayerPalette.Length; i++)
        {
            if (!ColorsMatch(color, PlayerPalette[i]))
                continue;

            matchesPalette = true;
            return true;
        }

        return false;
    }

    private static Color GetFirstAvailableColor(List<Color> usedColors)
    {
        for (int i = 0; i < PlayerPalette.Length; i++)
        {
            if (!ContainsColor(usedColors, PlayerPalette[i]))
                return PlayerPalette[i];
        }

        return Color.white;
    }

    private static Color GetDefaultColor(int index)
    {
        return PlayerPalette[Mathf.Abs(index) % PlayerPalette.Length];
    }

    private static bool ContainsColor(List<Color> colors, Color color)
    {
        for (int i = 0; i < colors.Count; i++)
        {
            if (ColorsMatch(colors[i], color))
                return true;
        }

        return false;
    }

    private static bool ColorsMatch(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) <= 0.01f &&
            Mathf.Abs(a.g - b.g) <= 0.01f &&
            Mathf.Abs(a.b - b.b) <= 0.01f;
    }
}
