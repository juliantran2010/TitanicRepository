using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Cupboard : MonoBehaviour, IInteractable
{
    private InteractionType type = InteractionType.Use;
    public InteractionType Type => type;

    public string ObjectName => "Cupboard";

    [SerializeField] private Dialogue dialogue;

    public void Interact()
    {
        bool hasKey = Inventory.Instance.ContainsItem("Cupboard Key");
        var variables = new Dictionary<string, object>
        {
            { "hasKey", hasKey }
        };
        DialogueManager.Instance.StartDialogue(dialogue, variables);

        if (hasKey)
        {
            DialogueManager.Instance.OnDialogueEnd += OpenDoor;
        }
    }

    private void OpenDoor(Dialogue _dialogue)
    {
        if (_dialogue != dialogue) return;
        gameObject.transform.DORotate(new Vector3(0, -155, 0), 1f);
        DialogueManager.Instance.OnDialogueEnd -= OpenDoor;
        type = InteractionType.None; // Disable further interaction after opening the cupboard
    }
}
