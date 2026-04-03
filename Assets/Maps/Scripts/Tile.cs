using UnityEngine;

public class Tile : MonoBehaviour
{
    private Renderer rend;
    private Color originalColor;
    public bool isOccupied = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
    }

    void OnMouseOver()
    {
        rend.material.color = Color.yellow;
        Debug.Log("Over");
    }

    void OnMouseExit()
    {
        rend.material.color = originalColor;
    }

    void OnMouseDown()
    {
        rend.material.color = Color.red;
        Debug.Log("Down");
    }
}