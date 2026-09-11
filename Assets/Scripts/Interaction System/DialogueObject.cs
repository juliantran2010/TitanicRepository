using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class DialogueObject : InteractableObject
{
    [SerializeField] protected Dialogue dialogue;
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
        if (dialogue != null && dialogue.inkJSON != null)
        {
            dialogueManger.OnDialogueCompleted += OnDialogueEnd;
            dialogueManger.OnTriggerFound += OnInkTrigger;
            dialogueManger.StartDialogue(dialogue);
        }
    }

    protected virtual void OnDestroy()
    {
        if (dialogueManger == null) return;
        dialogueManger.OnDialogueCompleted -= OnDialogueEnd;
        dialogueManger.OnTriggerFound -= OnInkTrigger;
    }

    protected virtual void OnDialogueEnd(Dialogue _dialogue, Story _story)
    {
        if (dialogue != _dialogue) return;
        dialogue.dialogueState = _story.state.ToJson();
        SetPersistentStateValue("dialogue_state", dialogue.dialogueState);
        dialogueManger.OnDialogueCompleted -= OnDialogueEnd;
        dialogueManger.OnTriggerFound -= OnInkTrigger;
    }

    protected override void OnStateRestored()
    {
        dialogue.dialogueState = GetPersistentStateValue("dialogue_state", string.Empty);
    }

    protected virtual void OnInkTrigger(string triggerName) { }
}
