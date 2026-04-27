using UnityEngine;

public enum GameState
{
    Playing,
    Paused,
    Ended
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public GameState CurrentState = GameState.Playing;

    void Awake()
    {
        Instance = this;
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
    }
}