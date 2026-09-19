using UnityEngine;
using UnityEngine.InputSystem;

public class Tutorial : MonoBehaviour
{
    [SerializeField] private string MoveQuestId = "tut_move";
    [SerializeField] private float requiredWalkDuration = 1.2f;
    private float currentWalkTime = 0f;
    private bool isTrackingMovement = true;

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

        // Prüft, ob mindestens eine der vier Tasten gedrückt gehalten wird
        bool isMoving = keyboard.wKey.isPressed ||
                        keyboard.aKey.isPressed ||
                        keyboard.sKey.isPressed ||
                        keyboard.dKey.isPressed;

        if (isMoving)
        {
            currentWalkTime += Time.deltaTime;

            // Fertig, sobald die benötigte Zeit mit einer beliebigen Taste erreicht ist
            if (currentWalkTime >= requiredWalkDuration)
            {
                QuestManager.Instance.CompleteQuest(MoveQuestId);
                isTrackingMovement = false;
            }
        }
    }

    private void HandleQuestCompleted(string questId)
    {
        if (questId == MoveQuestId)
        {
                QuestManager.Instance.AddQuest("tut_interact", "Click on the computer using [Left Click]");
                InteractionManager.Instance.OnInteracted += HandleInteracted;
        }
        else if (questId == "binoculars")
        {
                QuestManager.Instance.AddQuest("tut_binoculars", "Use the binoculars with [Right Click]");
        }
    }

    private void HandleInteracted(InteractableObject interactable)
    {
        InteractionManager.Instance.OnInteracted -= HandleInteracted;
        QuestManager.Instance.CompleteQuest("tut_interact");
    }
}