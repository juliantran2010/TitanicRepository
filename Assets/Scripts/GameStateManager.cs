using Ink.Runtime;
using System;
using System.Collections;
using UnityEngine;

public enum GameState
{
    Intro, Gameplay, Inspect, Dialogue, PauseMenu, Loading
}
public class GameStateManager : MonoBehaviour
{
    public bool IsTest = false;
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    public Action<GameState, GameState> OnStateChanged;

    [SerializeField] private Dialogue introDialogue;
    [SerializeField] private Dialogue cabinDialogue;

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

    private IEnumerator Start()
    {
        yield return null; // Warten, bis alle Start-Methoden aufgerufen wurden
        if (IsTest)
        {
            SetState(GameState.Gameplay);
            DialogueManager.Instance.StartDialogue(cabinDialogue);
        }
        else
        {
            StartIntro();
        }
    }

    private void StartIntro()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[GameStateManager] Kein DialogueManager in der Szene gefunden!");
            return;
        }
        SetState(GameState.Intro);
        DialogueManager.Instance.StartDialogue(introDialogue, (story) =>
        {
            SetState(GameState.Gameplay);
        });
        GameSceneManager.Instance.OnSceneChanged += HandleSceneChanged;
    }

    private void HandleSceneChanged(string sceneName)
    {
        if (sceneName != "CabinScene") return;

        DialogueManager.Instance.StartDialogue(cabinDialogue);
        GameSceneManager.Instance.OnSceneChanged -= HandleSceneChanged;
    }


    public void SetState(GameState newState)
    {
        GameState oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
        //Debug.Log($"[GameStateManager] State changed from {oldState} to {newState}");
    }
}
