using System;
using System.Collections.Generic;
using UnityEngine;

// Holds every component the player owns but hasn't equipped yet. Equipped components live on
// CharacterEquipmentManager instead and don't count against this capacity, same way a deployed
// champion doesn't count against PlayerRoster's bench capacity.
public class PlayerItemBank : MonoBehaviour
{
    public int capacity = 15;

    [Header("Debug (no shop/events yet - use this to seed items for testing)")]
    public List<ItemComponentData> debugSeedItems = new List<ItemComponentData>();
    public bool debugFillOnStart = false;

    readonly List<ItemComponentData> items = new List<ItemComponentData>();

    public event Action OnBankChanged;

    public IReadOnlyList<ItemComponentData> Items => items;
    public bool IsFull => items.Count >= capacity;

    void Start()
    {
        if (debugFillOnStart) DebugAddSeedItems();
    }

    public ItemComponentData GetAt(int index)
    {
        return index >= 0 && index < items.Count ? items[index] : null;
    }

    public bool Add(ItemComponentData item)
    {
        if (item == null || IsFull) return false;

        items.Add(item);
        OnBankChanged?.Invoke();
        return true;
    }

    public bool RemoveAt(int index, out ItemComponentData removed)
    {
        removed = GetAt(index);
        if (removed == null) return false;

        items.RemoveAt(index);
        OnBankChanged?.Invoke();
        return true;
    }

    // Right-click the component header in Play Mode (the ⋮ menu also works) to run this anytime -
    // stand-in for a shop/event grant until those systems exist.
    [ContextMenu("Debug: Add Seed Items")]
    void DebugAddSeedItems()
    {
        foreach (ItemComponentData item in debugSeedItems)
            Add(item);
    }
}