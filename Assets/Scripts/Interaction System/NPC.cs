using System.Collections.Generic;
using UnityEngine;

public class NPC : InteractableObject
{
    [SerializeField] private string npcName;
    public override string ObjectName => npcName;

    [SerializeField] private Dialogue dialogue;
    public override InteractionType Type => InteractionType.Dialogue;
    public override void Interact()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }
}
