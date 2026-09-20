using Ink.Runtime;
using System;
using System.Collections;
using UnityEngine;

public enum GameState
{
    Intro, Gameplay, Inspect, Dialogue, PauseMenu, Loading, Minigame
}
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [SerializeField] private GameState currentState;
    public GameState CurrentState => currentState;
    public Action<GameState, GameState> OnStateChanged;

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

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState oldState = CurrentState;
        currentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
    }
}