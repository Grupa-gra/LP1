using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public float timeLeft = 300f;
    public TextMeshProUGUI timerText;

    // 1. Dodajemy referencj� do Twojego prawdziwego Managera
    public EndGameManager endGameManager;

    private bool isGameEnded = false;
    private bool isTimerRunning = false;

    void Start()
    {
        UpdateTimerUI();
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
            // Czas min��!
            timeLeft = 0;
            UpdateTimerUI();
            isGameEnded = true;

            // 2. Wo�amy EndGameManager, �eby zrobi� ca�� magi� z punktami i UI
            if (endGameManager != null)
            {
                endGameManager.EndGame();
            }
            else
            {
                Debug.LogError("Brak podpi�tego EndGameManager w Timerze!");
            }
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
}