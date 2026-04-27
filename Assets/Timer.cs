using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float timeLeft = 300f;
    public TextMeshProUGUI timerText;

    public GameObject endGameUI;
    public MonoBehaviour playerMovement;

    private bool isGameEnded = false;
    private bool isTimerRunning = false;

    void Start()
    {
        UpdateTimerUI();
        endGameUI.SetActive(false);
    }

    void Update()
    {
        if (isGameEnded || !isTimerRunning) return;

        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            EndGame();
        }
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    void UpdateTimerUI()
    {
        float minutes = Mathf.FloorToInt(timeLeft / 60);
        float seconds = Mathf.FloorToInt(timeLeft % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void EndGame()
    {
        isGameEnded = true;
        timeLeft = 0;
        UpdateTimerUI();

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