using NUnit.Framework.Interfaces;
using UnityEngine;

public class PickupObject : InteractableObject
{
    public override InteractionType Type => InteractionType.Pickup;

    [SerializeField] string objectName;
    public override string ObjectName => objectName;

    private void Start()
    {
        if (InteractionManager.Instance.HasInteracted(UniqueID))
        {
            Destroy(gameObject);
        }
    }

    public override void Interact()
    {
        bool wasAdded = Inventory.Instance.AddItem(this);
        if (wasAdded)
        {
            transform.SetParent(Inventory.Instance.transform);
            AttachToCameraField attachScript = gameObject.AddComponent<AttachToCameraField>();
        }
    }
}
