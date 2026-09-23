using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public event Action OnInventoryChanged;
    public event Action<string> OnItemAdded;
    public event Action<string> OnItemRemoved;
    [SerializeField] private List<PickupObject> items = new List<PickupObject>();
    public IReadOnlyList<PickupObject> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        foreach(PickupObject item in items)
        {
            OnItemAdded?.Invoke(item.ObjectName);
        }
    }

    public bool AddItem(PickupObject item)
    {
        items.Add(item);
        OnInventoryChanged?.Invoke(); // UI aktualisieren
        OnItemAdded?.Invoke(item.ObjectName);
        return true;
    }

    public void RemoveItem(string itemName, bool destoryObject = true)
    {
        var itemToRemove = items.FirstOrDefault(i => i.ObjectName == itemName);
        if (itemToRemove != null)
        {
            RemoveItem(itemToRemove, destoryObject);
        }
    }
    public void RemoveItem(PickupObject item, bool destoryObject = true)
    {
        if (items.Remove(item))
        {
            if (destoryObject)
                Destroy(item.gameObject);
            OnItemRemoved?.Invoke(item.ObjectName);
            OnInventoryChanged?.Invoke(); // UI aktualisieren
        }
    }

    public bool ContainsItem(string itemName)
    {
        return items.Any(i => i.ObjectName == itemName);
    }

    public PickupObject GetItem(string itemName)
    {
        return items.FirstOrDefault(i => i.ObjectName == itemName);
    }
}
