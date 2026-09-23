using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class DialogueObject : InteractableObject
{
    [Header("Dialogue Options")]
    [SerializeField] protected Dialogue dialogue;
    [SerializeField] protected Dialogue DialogeIfCannotInteract;
    public override InteractionType Type => dialogue.IsEmpty() ? InteractionType.None : InteractionType.Dialogue;

    protected DialogueManager dialogueManger;

    protected override void Start()
    {
        base.Start();
        dialogueManger = DialogueManager.Instance;
    }
    protected override void OnInteract()
    {
        StartDialogue(dialogue);   
    }

    protected override void OnCannotInteract()
    {
        StartDialogue(DialogeIfCannotInteract);
    }

    private void StartDialogue(Dialogue currentDialogue)
    {
        if (dialogueManger == null) return;
        dialogueManger.StartDialogue(
            currentDialogue,
            (story) => {
                currentDialogue.dialogueState = story.state.ToJson();
                SetPersistentStateValue("dialogue_state", currentDialogue.dialogueState);
                OnDialogueEnd(currentDialogue, story);
                lastInteractionTime = Time.time;
            },
            OnInkTrigger
        );
    }

    protected virtual void OnDialogueEnd(Dialogue dialogue, Story story) 
    {
        ZoomOut();
    }

    protected override void OnStateRestored()
    {
        dialogue.dialogueState = GetPersistentStateValue("dialogue_state", string.Empty);
    }

    protected virtual void OnInkTrigger(string triggerName) { }
}
