using UnityEngine;


public enum InteractionType
{
    Dialogue,
    Pickup,
    Inspect,
    Use,
    Teleport,
    None
}
public interface IInteractable
{
    public InteractionType Type { get; }
    public string ObjectName { get; }
    void Interact();
}
