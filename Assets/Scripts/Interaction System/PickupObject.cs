using NUnit.Framework.Interfaces;
using UnityEngine;

public class PickupObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Pickup;

    [SerializeField] private Dialogue dialogueAfterPickup;

    protected override void OnInteract()
    {
        bool wasAdded = Inventory.Instance.AddItem(this);
        if (wasAdded)
        {
            transform.SetParent(Inventory.Instance.transform);
            AttachToCameraField attachScript = gameObject.AddComponent<AttachToCameraField>();
            if (dialogueAfterPickup != null && dialogueAfterPickup.inkJSON != null)
            {
                DialogueManager.Instance.StartDialogue(dialogueAfterPickup);
            }
        }
    }

    protected override void OnStateRestored()
    {
        if (HasInteracted)
        {
            Destroy(gameObject);
        }
    }
}
