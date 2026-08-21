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
        #if UNITY_EDITOR
        // 1. Wenn es das Prefab-Asset im Projektordner ist -> ID IMMER LÖSCHEN/LEER HALTEN!
        if (!gameObject.scene.IsValid())
        {
            if (!string.IsNullOrEmpty(uniqueID))
            {
                uniqueID = string.Empty;
                UnityEditor.EditorUtility.SetDirty(this);
            }
            return;
        }

        // 2. Wenn es eine Instanz in der Szene ist und keine ID hat -> NEUE GENERIEREN!
        if (string.IsNullOrEmpty(uniqueID))
        {
            uniqueID = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        #endif
    }


    public abstract InteractionType Type { get; }
    public abstract string ObjectName { get; }

    public abstract void Interact();

}
