using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    public GameObject pauseCanvas;

    bool paused;

    void Start()
    {
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            if (paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (pauseCanvas != null)
            pauseCanvas.SetActive(true);
        Time.timeScale = 0f;
        paused = true;
    }



    public void ResumeGame()
    {
        if (pauseCanvas != null)
            pauseCanvas.SetActive(false);
        Time.timeScale = 1f;
        paused = false;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameDemo");
    }

}
