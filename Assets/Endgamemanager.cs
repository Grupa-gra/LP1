using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameManager : MonoBehaviour
{
    public GameObject endGameUI;
    public MonoBehaviour playerMovement;
    public PauseMenu pauseMenu;

    public void EndGame()
    {
        GameStateManager.Instance.SetState(GameState.Ended);
        pauseMenu.ForceClosePause();

        playerMovement.enabled = false;

        endGameUI.SetActive(true);
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}