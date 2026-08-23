using Ink.Runtime;
using System.Collections.Generic;
using UnityEngine;

public class DialogueObject : InteractableObject
{
    [SerializeField] private string objectName;
    public override string ObjectName => objectName;

    [SerializeField] private Dialogue dialogue;
    public override InteractionType Type => InteractionType.Dialogue;
    protected override void OnInteract()
    {
        DialogueManager manager = DialogueManager.Instance;
        if (manager == null) return;
        if (dialogue != null && dialogue.inkJSON != null)
        {
            manager.OnDialogueEnd += OnDialogueEnd;
            manager.StartDialogue(dialogue);
        }
    }

    private void OnDestroy()
    {
        DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
    }

    protected virtual void OnDialogueEnd(Dialogue _dialogue, Story _story)
    {
        if (dialogue != _dialogue) return;
        dialogue.dialogueState = _story.state.ToJson();
        SetStateValue("dialogue_state", dialogue.dialogueState);
        DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
    }

    protected override void OnStateRestored()
    {
        dialogue.dialogueState = GetStateValue<string>("dialogue_state");
    }
}
