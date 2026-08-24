using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class DialogueObject : InteractableObject
{
    [SerializeField] private string objectName;
    public override string ObjectName => objectName;

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
            dialogueManger.OnDialogueEnd += OnDialogueEnd;
            dialogueManger.OnTriggerFound += OnInkTrigger;
            dialogueManger.StartDialogue(dialogue, SetDialogueVariables());
        }
    }

    protected virtual Dictionary<string, object> SetDialogueVariables() { return null; }

    protected virtual void OnDestroy()
    {
        dialogueManger.OnDialogueEnd -= OnDialogueEnd;
        dialogueManger.OnTriggerFound -= OnInkTrigger;
    }

    protected virtual void OnDialogueEnd(Dialogue _dialogue, Story _story)
    {
        if (dialogue != _dialogue) return;
        dialogue.dialogueState = _story.state.ToJson();
        SetPersistentStateValue("dialogue_state", dialogue.dialogueState);
        dialogueManger.OnDialogueEnd -= OnDialogueEnd;
        dialogueManger.OnTriggerFound -= OnInkTrigger;
    }

    protected override void OnStateRestored()
    {
        dialogue.dialogueState = GetPersistentStateValue("dialogue_state", string.Empty);
    }

    protected virtual void OnInkTrigger(string triggerName) { }
}
