using UnityEngine;

public enum InteractionType
{
    Dialogue,
    Pickup,
    Inspect,
    Use,
    Teleport,
    Read,
    None
}
public abstract class InteractableObject : MonoBehaviour
{
    [SerializeField] private string uniqueID;
    public string UniqueID => uniqueID;
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }


    public abstract InteractionType Type { get; }
    public abstract string ObjectName { get; }

    public abstract void Interact();

}
