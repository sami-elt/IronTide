using UnityEngine;
using UnityEngine.UI;

public class IconPickerUI : MonoBehaviour
{
    public Transform container;
    public GameObject iconButtonPrefab;
    public Sprite[] icons;
    public PlayerSetupUI manager;

    void Start()
    {
        GenerateIcons();
    }

    void GenerateIcons()
    {
        if (container == null || iconButtonPrefab == null || icons == null)
        {
            Debug.LogError("IconPickerUI: Missing references!");
            return;
        }

        foreach (var icon in icons)
        {
            GameObject btn = Instantiate(iconButtonPrefab, container);

            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.sprite = icon;

            IconButton iconBtn = btn.GetComponent<IconButton>();
            if (iconBtn != null)
            {
                iconBtn.icon = icon;
                iconBtn.manager = manager;
            }
        }
    }
}
