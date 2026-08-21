using NUnit.Framework.Interfaces;
using UnityEngine;

public class PickupObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Pickup;

    [SerializeField] string objectName;
    public override string ObjectName => objectName;

    protected override void OnInteract()
    {
        bool wasAdded = Inventory.Instance.AddItem(this);
        if (wasAdded)
        {
            transform.SetParent(Inventory.Instance.transform);
            AttachToCameraField attachScript = gameObject.AddComponent<AttachToCameraField>();
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
