using UnityEngine;

public enum GameState
{
    Gameplay, Inspect, Dialogue, PauseMenu
}
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    // Events, auf die andere Skripte hören können
    public delegate void OnStateChangedDelegate(GameState newState);
    public event OnStateChangedDelegate OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetState(GameState.Gameplay);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}
