using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    public TMP_InputField nameInput;
    public Image iconImage;
    public Image colorImage;
    public GameObject colorPickerPanel;
    public PlayerSetupUI manager;
    public Sprite[] icons;
    private int currentIcon = 0;

    void Start()
    {
        // 🔥 sätt default icon så den aldrig är NULL
        if (icons != null && icons.Length > 0)
        {
            iconImage.sprite = icons[0];
            colorImage.color = Random.ColorHSV();
        }
    }

    public PlayerData GetData()
    {
        return new PlayerData
        {
            playerName = nameInput.text,
            playerColor = colorImage.color,
            icon = iconImage.sprite
        };
    }

    public void OpenColorPicker()
    {
        if (manager == null)
        {
            Debug.LogError("Manager NOT assigned!");
            return;
        }

        manager.SetCurrentSlot(this);
        manager.colorPickerPanel.SetActive(true);
    }

    public void OpenIconPicker()
    {
        manager.SetCurrentSlot(this);
        manager.iconPickerPanel.SetActive(true);
    }

    public void SetColor(Color newColor)
    {
        colorImage.color = newColor;
        colorPickerPanel.SetActive(false);
    }

    public void SetIcon(Sprite newIcon)
    {
        if (iconImage == null)
        {
            Debug.LogError($"iconImage not assigned on {gameObject.name}; cannot set icon.");
            return;
        }

        if (newIcon == null)
        {
            Debug.LogWarning($"Attempted to set a NULL icon on {gameObject.name}; clearing sprite.");
            iconImage.sprite = null;
            return;
        }

        iconImage.sprite = newIcon;
    }




}