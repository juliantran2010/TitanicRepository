using System.Collections.Generic;
using UnityEngine;

public enum InteractionType
{
    Dialogue, Pickup, Inspect, Use, Teleport, Read, None
}
public abstract class InteractableObject : MonoBehaviour
{
    [SerializeField] private string uniqueID;
    public string UniqueID => uniqueID;

    public string UniqueInteractionLabel = "";
    protected void OnValidate()
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
    [SerializeField] private string objectName;
    public virtual string ObjectName => objectName;
    [SerializeField] private string displayName;
    public virtual string DisplayName => displayName;
    private Dictionary<string, object> _localState = new Dictionary<string, object>();
    public bool HasInteracted => GetPersistentStateValue<bool>("has_interacted", false);

    [SerializeField] private Dialogue DialogeIfCannotInteract;

    protected virtual void Start()
    {
        RestoreState();
    }
    public void Interact()
    {
        if (DialogeIfCannotInteract.inkJSON != null && (!QuestManager.Instance.TryGetVariable("can_interact_" + ObjectName, out object canInteract) || !(bool)canInteract))
        {
            DialogueManager.Instance.StartDialogue(DialogeIfCannotInteract);
            return;
        }
        else
        {
            SetPersistentStateValue("has_interacted", true);
            OnInteract();
        }
    }

    protected abstract void OnInteract();
    protected void SetPersistentStateValue<T>(string key, T value)
    {
        _localState[key] = value;
        SaveCurrentState();
    }
    protected T GetPersistentStateValue<T>(string key, T defaultValue = default)
    {
        if (_localState.TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    private void SaveCurrentState()
    {
        if (SaveManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            SaveManager.Instance.SaveObjectState(uniqueID, _localState);
        }
    }

    private void RestoreState()
    {
        if (SaveManager.Instance == null || string.IsNullOrEmpty(uniqueID)) return;

        var savedState = SaveManager.Instance.LoadObjectState(uniqueID);
        if (savedState != null)
        {
            _localState = new Dictionary<string, object>(savedState);
            OnStateRestored();
        }
    }
    protected virtual void OnStateRestored() { }

}
