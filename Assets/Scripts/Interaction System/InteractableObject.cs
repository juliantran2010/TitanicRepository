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
        // Funktioniert nur im Unity-Editor
#if UNITY_EDITOR
        // Ignoriere Prefabs im Projekt-Ordner, betrifft nur Objekte in der Szene
        if (!gameObject.scene.IsValid()) return;

        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();

            // WICHTIG: Sagt Unity, dass das Objekt UND die Szene geänderten Speichercode haben
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }


    public abstract InteractionType Type { get; }
    public abstract string ObjectName { get; }

    public abstract void Interact();

}
