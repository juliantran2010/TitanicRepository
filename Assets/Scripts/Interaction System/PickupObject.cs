using NUnit.Framework.Interfaces;
using UnityEngine;

public class PickupObject : MonoBehaviour, IInteractable
{
    public InteractionType Type => InteractionType.Pickup;

    [SerializeField] string objectName;
    public string ObjectName => objectName;

    private void Start()
    {
        if (Inventory.Instance.ContainsItem(objectName))
        {
            Destroy(gameObject);
        }
    }

    public void Interact()
    {
        bool wasAdded = Inventory.Instance.AddItem(this);
        if (wasAdded)
        {
            transform.SetParent(Inventory.Instance.transform);
            AttachToCameraField attachScript = gameObject.AddComponent<AttachToCameraField>();
        }
    }
}
