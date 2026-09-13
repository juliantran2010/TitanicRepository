using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class DialogueObject : InteractableObject
{
    [SerializeField] protected Dialogue dialogue;
    [SerializeField] protected Dialogue DialogeIfCannotInteract;
    protected bool CanInteract => !(DialogeIfCannotInteract.inkJSON != null && (!QuestManager.Instance.TryGetVariable("can_interact_" + ObjectName, out object canInteract) || !(bool)canInteract));
    public override InteractionType Type => (dialogue != null && dialogue.inkJSON != null) ? InteractionType.Dialogue : InteractionType.None;

    protected DialogueManager dialogueManger;

    protected override void Start()
    {
        base.Start();
        dialogueManger = DialogueManager.Instance;
    }
    protected override void OnInteract()
    {
        if (dialogueManger == null) return;
        Dialogue currentDialogue = CanInteract ? dialogue : DialogeIfCannotInteract;
        if (currentDialogue != null && currentDialogue.inkJSON != null)
        {
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
    }

    protected virtual void OnDialogueEnd(Dialogue dialogue, Story story) { }

    protected override void OnStateRestored()
    {
        dialogue.dialogueState = GetPersistentStateValue("dialogue_state", string.Empty);
    }

    protected virtual void OnInkTrigger(string triggerName) { }
}
