using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    public TMP_InputField nameInput;
    public Image colorImage;
    public GameObject colorPickerPanel;
    public PlayerSetupUI manager;

    public PlayerData GetData()
    {
        return new PlayerData
        {
            playerName = nameInput.text,
            playerColor = colorImage.color
        };
    }

    public Color CurrentColor => colorImage != null ? colorImage.color : Color.clear;
    public bool IsActive => gameObject.activeSelf;

    public void OpenColorPicker()
    {
        if (manager == null)
        {
            Debug.LogError("Manager NOT assigned!");
            return;
        }

        manager.SetCurrentSlot(this);
        if (manager.colorPickerPanel != null)
            manager.colorPickerPanel.SetActive(true);
    }

    public void SetColor(Color newColor)
    {
        SetColorSilently(newColor);
        if (colorPickerPanel != null)
            colorPickerPanel.SetActive(false);
    }

    public void SetColorSilently(Color newColor)
    {
        if (colorImage != null)
            colorImage.color = newColor;
    }
}
