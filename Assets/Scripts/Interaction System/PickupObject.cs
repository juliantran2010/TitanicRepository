using NUnit.Framework.Interfaces;
using UnityEngine;

public class PickupObject : DialogueObject
{
    public override InteractionType Type => InteractionType.Pickup;

    protected override void OnInteract()
    {
        base.OnInteract();
        if (!CanInteract) return;
        bool wasAdded = Inventory.Instance.AddItem(this);
        if (wasAdded)
        {
            transform.SetParent(Inventory.Instance.transform);
            AttachToCameraField attachScript = gameObject.AddComponent<AttachToCameraField>();
        }
    }

    protected override void OnStateRestored()
    {
        base.OnStateRestored();
        if (HasInteracted)
        {
            Destroy(gameObject);
        }
    }
}
