using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseUI;
    private bool isPaused = false;
    public MonoBehaviour PlayerMovement;

    void Update()
    {
        if (GameStateManager.Instance.CurrentState != GameState.Playing &&
            GameStateManager.Instance.CurrentState != GameState.Paused)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    void Pause()
    {
        //PlayerMovement.enabled = false;
        pauseUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        GameStateManager.Instance.SetState(GameState.Paused);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        //PlayerMovement.enabled = true;
        pauseUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        GameStateManager.Instance.SetState(GameState.Playing);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ForceClosePause()
    {
        pauseUI.SetActive(false);
        isPaused = false;
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}