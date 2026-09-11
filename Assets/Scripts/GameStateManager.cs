using Ink.Runtime;
using System;
using System.Collections;
using UnityEngine;

public enum GameState
{
    Intro, Gameplay, Inspect, Dialogue, PauseMenu
}
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    //// Events, auf die andere Skripte hören können
    //public delegate void OnStateChangedDelegate(GameState newState);
    //public event OnStateChangedDelegate OnStateChanged;

    public Action<GameState, GameState> OnStateChanged;

    [SerializeField] private Dialogue introDialogue;

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
        StartIntro();
    }

    private void StartIntro()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[GameStateManager] Kein DialogueManager in der Szene gefunden!");
            return;
        }
        SetState(GameState.Intro);
        DialogueManager.Instance.OnDialogueCompleted += FinishIntro;
        DialogueManager.Instance.StartDialogue(introDialogue);
    }

    private void FinishIntro(Dialogue dialogue, Story story)
    {
        if (dialogue == introDialogue)
        {
            SetState(GameState.Gameplay);
            DialogueManager.Instance.OnDialogueCompleted -= FinishIntro;
        }
    }


    public void SetState(GameState newState)
    {
        GameState oldState = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(oldState, newState);
    }
}
