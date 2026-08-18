using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public event Action OnInventoryChanged;
    [SerializeField] private List<PickupObject> items = new List<PickupObject>();
    public IReadOnlyList<PickupObject> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AddItem(PickupObject item)
    {
        items.Add(item);
        OnInventoryChanged?.Invoke(); // UI aktualisieren
        return true;
    }

    public void RemoveItem(PickupObject item)
    {
        if (items.Remove(item))
        {
            OnInventoryChanged?.Invoke(); // UI aktualisieren
        }
    }

    public bool ContainsItem(string itemName)
    {
        return items.Any(i => i.ObjectName == itemName);
    }
}
