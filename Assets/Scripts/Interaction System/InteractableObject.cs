using System;
using System.Collections.Generic;
using UnityEngine;


public enum InteractionType
{
    Dialogue, Pickup, Inspect, Use, Teleport, Read, None, Undefined, Move, Open
}
public abstract class InteractableObject : MonoBehaviour
{
    [Serializable]
    public class VariablesEntry
    {
        public string name;
        public bool value;
    }
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

    [Header("Interaction State")]
    [SerializeField] private string uniqueID;
    public string UniqueID => uniqueID;
    [SerializeField] protected float interactionCooldown = 0.5f;
    protected float lastInteractionTime = -Mathf.Infinity;
    public bool InteractionIsOnCooldown => Time.time < lastInteractionTime + interactionCooldown;
    [SerializeField] protected string neccessaryQuestIdFinished;
    [SerializeField] protected VariablesEntry[] necessaryVariablesSet;
    private Dictionary<string, object> _localState = new Dictionary<string, object>();
    public bool HasInteracted => GetPersistentStateValue<bool>("has_interacted", false);
    [SerializeField] protected string progressQuestId;

    [Header("Interaction UI")]
    [SerializeField] protected string objectName;
    public virtual string ObjectName => objectName;
    [SerializeField] protected string displayName;
    public virtual string DisplayName => displayName;
    public string UniqueInteractionLabel = "";
    public InteractionType UniqueInteractionType = InteractionType.Undefined;
    public abstract InteractionType Type { get; }

    [SerializeField] private bool _showLabel = true;
    public bool ShowLabel
    {
        get => _showLabel;
        set
        {
            _showLabel = value;
            SetPersistentStateValue("show_label", value);
        }
    }
    [SerializeField] public bool ShowLabelIfCannotInteract = false;
    [SerializeField] private bool _isInteractable = true;
    public bool IsInteractable
    {
        get => _isInteractable;
        set
        {
            _isInteractable = value;
            SetPersistentStateValue("is_interactable", this._isInteractable);
            if (InteractionManager.Instance != null)
                InteractionManager.Instance.ResetInteractionOverlay();
        }
    }

    [Header("Optional: Camera Focus")]
    [SerializeField] protected Transform cameraFocusTarget;

    public bool CanInteract()
    {
        if (!IsInteractable) return false;
        QuestManager questManager = QuestManager.Instance;
        if (!string.IsNullOrWhiteSpace(neccessaryQuestIdFinished) && !questManager.IsQuestCompleted(neccessaryQuestIdFinished.Trim()))
            return false;
        if (InteractionIsOnCooldown) return false;
        if (necessaryVariablesSet != null)
        {
            foreach (VariablesEntry entry in necessaryVariablesSet)
            {
                if (!QuestManager.Instance.TryGetVariable(entry.name, out object val) || (bool)val != entry.value)
                    return false;
            }
        }
        return true;
    }


    protected virtual void Start()
    {
        RestoreState();
    }
    public void Interact()
    {
        if (InteractionIsOnCooldown) return;
        if (!CanInteract())
        {
            OnCannotInteract();
            return;
        }

        lastInteractionTime = Time.time;
        SetPersistentStateValue("has_interacted", true);

        ZoomIn();
        OnInteract();
    }
    protected void ZoomIn()
    {
        if (cameraFocusTarget != null)
        {
            CameraFocusMover mover = CameraFocusMover.Instance;
            if (mover != null)
            {
                mover.ZoomIn(cameraFocusTarget);
            }
        }
    }
    protected void ZoomOut()
    {
        if (cameraFocusTarget != null)
        {
            CameraFocusMover mover = CameraFocusMover.Instance;
            if (mover != null)
            {
                mover.ZoomOut();
            }
        }
    }
    protected virtual void OnCannotInteract(){}

    protected abstract void OnInteract();
    protected bool ContainsPersistentState(params string[] keys)
    {
        bool containsKeys = true;
        foreach (string key in keys)
        {
            if (!_localState.ContainsKey(key))
                containsKeys = false;
        }
        return containsKeys;
    }
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
            if (ContainsPersistentState("is_interactable"))
                _isInteractable = GetPersistentStateValue<bool>("is_interactable");
            if (ContainsPersistentState("show_label"))
                _showLabel = GetPersistentStateValue<bool>("show_label");
            OnStateRestored();
        }
    }
    protected virtual void OnStateRestored() { }

}
