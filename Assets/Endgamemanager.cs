// using TMPro;
// using UnityEngine;
// using UnityEngine.SceneManagement;
//
// public class EndGameManager : MonoBehaviour
// {
//     public GameObject endGameUI;
//     public MonoBehaviour playerMovement;
//     public PauseMenu pauseMenu;
//
//     [Header("Score")]
//     [SerializeField] private TextMeshProUGUI finalScoreText;
//
//     private bool hasEnded = false;
//
//     public void EndGame()
//     {
//         if (hasEnded) return;
//         hasEnded = true;
//
//         Debug.Log("ENDGAME SCORE RAW: " + CollectibleItemGlob.ScoreCount);
//
//         GameStateManager.Instance.SetState(GameState.Ended);
//
//         pauseMenu.ForceClosePause();
//
//         playerMovement.enabled = false;
//
//         endGameUI.SetActive(true);
//
//         Debug.Log("ENDGAME SCORE BEFORE TEXT: " + CollectibleItemGlob.ScoreCount);
//
//         finalScoreText.text = "Final Score: " + CollectibleItemGlob.ScoreCount;
//
//         Debug.Log("TEXT SET");
//
//         Time.timeScale = 0f;
//
//         Cursor.lockState = CursorLockMode.None;
//         Cursor.visible = true;
//     }
//
//     public void Retry()
//     {
//         Time.timeScale = 1f;
//         CollectibleItemGlob.ScoreCount = 0;
//         SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
//     }
//
//     public void LoadMenu()
//     {
//         Time.timeScale = 1f;
//         CollectibleItemGlob.ScoreCount = 0;
//         SceneManager.LoadScene("Menu");
//     }
// }
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameManager : MonoBehaviour
{
    public GameObject endGameUI;
    public MonoBehaviour playerMovement;
    public PauseMenu pauseMenu;

    [Header("Score")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Leaderboard Result")]
    [Tooltip("Opcjonalny tekst informujący gracza czy wynik trafił na tablicę.")]
    [SerializeField] private TextMeshProUGUI leaderboardInfoText;

    private bool hasEnded = false;

    public void EndGame()
    {
        if (hasEnded) return;
        hasEnded = true;

        GameStateManager.Instance.SetState(GameState.Ended);

        pauseMenu.ForceClosePause();

        playerMovement.enabled = false;

        endGameUI.SetActive(true);

        int finalScore = CollectibleItemGlob.ScoreCount;
        finalScoreText.text = "Final Score: " + finalScore;

        // --- Zapis do leaderboardu ---
        if (LeaderboardManager.Instance != null)
        {
            bool saved = LeaderboardManager.Instance.TryAddScore(finalScore);

            if (leaderboardInfoText != null)
                leaderboardInfoText.text = saved
                    ? "Nowy wynik w Top 10!"
                    : "Wynik nie trafił do Top 10.";
        }

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        CollectibleItemGlob.ScoreCount = 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        CollectibleItemGlob.ScoreCount = 0;
        SceneManager.LoadScene("Menu");
    }
}
