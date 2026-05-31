using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    
    public GameObject pauseCanvas;

    bool paused;

    void Start()
    {
        Debug.Log("PauseManager startad");
        pauseCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Esc funkar");
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
        pauseCanvas.SetActive(true);
        Time.timeScale = 0f;
        paused = true;
    }



    public void ResumeGame()
    {
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f;
        paused = false;
    }
    // load main scen
    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameDemo");
    }

}
