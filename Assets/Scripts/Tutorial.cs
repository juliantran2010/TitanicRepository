using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Tutorial : MonoBehaviour
{
    private const string MoveQuestId = "tut_move";
    [SerializeField] private float requiredWalkDuration = 1.2f;
    private float currentWalkTime = 0f;
    private bool isTrackingMovement = true;
    private readonly bool[] keysPressed = new bool[4];

    void Start()
    {
        GameStateManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        if (oldState == GameState.Intro && newState == GameState.Gameplay)
        {
            StartTutorial();
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    public void StartTutorial()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogError("QuestManager instance is not found. Make sure QuestManager is initialized before Tutorial.");
            return;
        }
        QuestManager.Instance.AddQuest(MoveQuestId, "Move with [W, A, S, D]");
        QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            InteractionManager.Instance.OnInteracted -= HandleInteracted;
        }
    }

    private void Update()
    {
        TrackMovement();
    }

    private void TrackMovement()
    {
        if (!isTrackingMovement) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        KeyControl[] keys = { keyboard.wKey, keyboard.aKey, keyboard.sKey, keyboard.dKey };

        bool isMoving = false;

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i].isPressed)
            {
                keysPressed[i] = true;
                isMoving = true;
            }
        }

        if (isMoving)
        {
            currentWalkTime += Time.deltaTime;
        }

        // Fertig, wenn alle 4 Indizes true sind und die Zeit reicht
        if (currentWalkTime >= requiredWalkDuration && keysPressed.All(pressed => pressed))
        {
            QuestManager.Instance.CompleteQuest(MoveQuestId);
            isTrackingMovement = false;
        }
    }
    private void HandleQuestCompleted(string questId)
    {
        switch (questId)
        {
            case MoveQuestId:
                QuestManager.Instance.AddQuest("tut_interact", "Interact with people/objects using [Left Click]");
                InteractionManager.Instance.OnInteracted += HandleInteracted;
                break;
            case "binoculars":
                QuestManager.Instance.AddQuest("tut_binoculars", "Use the binoculars with [Right Click]");
                break;
        }
    }

    private void HandleInteracted(InteractableObject interactable)
    {
        InteractionManager.Instance.OnInteracted -= HandleInteracted;
        QuestManager.Instance.CompleteQuest("tut_interact");
    }
}
