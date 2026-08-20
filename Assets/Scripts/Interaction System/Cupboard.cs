using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Cupboard : InteractableObject
{
    private InteractionType type = InteractionType.Use;
    public override InteractionType Type => type;

    public override string ObjectName => "Cupboard";
    [SerializeField] private Dialogue dialogue;
    public override void Interact()
    {
        bool hasKey = Inventory.Instance.ContainsItem("Cupboard Key");
        var variables = new Dictionary<string, object>
        {
            { "hasKey", hasKey }
        };
        DialogueManager.Instance.OnTriggerFound += OpenDoor;
        DialogueManager.Instance.StartDialogue(dialogue, variables);
    }

    private void OpenDoor(string triggerName)
    {
        if (triggerName != "open_cupboard") return;
        Inventory.Instance.RemoveItem("Cupboard Key");
        gameObject.transform.DORotate(new Vector3(0, -155, 0), 1f);
        DialogueManager.Instance.OnTriggerFound -= OpenDoor;
        type = InteractionType.None; // Disable further interaction after opening the cupboard
    }
}
