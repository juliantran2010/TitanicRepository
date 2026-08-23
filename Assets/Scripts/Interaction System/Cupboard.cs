using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Cupboard : DialogueObject
{
    private InteractionType type = InteractionType.Use;
    public override InteractionType Type => type;

    public override string ObjectName => "Cupboard";
    [SerializeField] private Dialogue dialogue;
    protected override void OnInteract()
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
        GetComponent<BoxCollider>().enabled = false; // Disable further interaction after opening the cupboard
        SetStateValue("isOpened", true);
    }

    protected override void OnStateRestored()
    {
        base.OnStateRestored();
        if (GetStateValue<bool>("isOpened"))
        {
            gameObject.transform.Rotate(new Vector3(0, -155, 0));
            GetComponent<BoxCollider>().enabled = false;
        }
    }
}
