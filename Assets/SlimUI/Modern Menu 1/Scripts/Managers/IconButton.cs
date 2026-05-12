using UnityEngine;

public class IconButton : MonoBehaviour
{
    public Sprite icon;
    public PlayerSetupUI manager;

    public void SelectIcon()
    {
        Debug.Log("btn clicked");
        manager.SelectIcon(icon);
    }
}