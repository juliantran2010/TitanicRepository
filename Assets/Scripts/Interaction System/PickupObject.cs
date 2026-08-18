using NUnit.Framework.Interfaces;
using UnityEngine;

public class PickupObject : MonoBehaviour, IInteractable
{
    public InteractionType Type => InteractionType.Pickup;

    [SerializeField] string objectName;
    public string ObjectName => objectName;

    public void Interact()
    {
        bool wasAdded = Inventory.Instance.AddItem(this);
        if (wasAdded)
        {
            transform.SetParent(Inventory.Instance.transform);
            gameObject.SetActive(false);
        }
    }
}
