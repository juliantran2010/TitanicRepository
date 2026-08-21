using System;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public event Action<string, Dictionary<string, object>> OnStateChanged;
    private Dictionary<string, Dictionary<string, object>> _objectStates = new Dictionary<string, Dictionary<string, object>>();

    public void SaveObjectState(string id, Dictionary<string, object> stateData)
    {
        _objectStates[id] = new Dictionary<string, object>(stateData);
        OnStateChanged?.Invoke(id, _objectStates[id]);
    }

    public Dictionary<string, object> LoadObjectState(string id)
    {
        if (_objectStates.TryGetValue(id, out var state))
        {
            return state;
        }
        return null;
    }

    public void ClearAllStates()
    {
        _objectStates.Clear();
    }
}
