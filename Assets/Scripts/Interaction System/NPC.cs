using System.Collections.Generic;
using UnityEngine;

public class NPC : InteractableObject
{
    [SerializeField] private string npcName;
    public override string ObjectName => npcName;

    [SerializeField] private Dialogue dialogue;
    public override InteractionType Type => InteractionType.Dialogue;
    protected override void OnInteract()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }
}
