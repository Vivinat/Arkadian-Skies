using System;
using System.Collections.Generic;
using UnityEngine;

// Holds every component the player owns but hasn't equipped yet. Equipped components live on
// CharacterEquipmentManager instead and don't count against this capacity, same way a deployed
// champion doesn't count against PlayerRoster's bench capacity.
public class PlayerItemBank : MonoBehaviour
{
    public int capacity = 15;
    public ItemRecipeBook recipeBook; // optional - enables combining components in the bank

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

    // Forges a complete item when the two dropped components form a known recipe: both
    // components are consumed and the result lands where the drop happened
    public bool TryCombine(int fromIndex, int toIndex)
    {
        if (recipeBook == null || fromIndex == toIndex) return false;

        ItemComponentData a = GetAt(fromIndex);
        ItemComponentData b = GetAt(toIndex);
        ItemComponentData result = recipeBook.FindRecipe(a, b);
        if (result == null) return false;

        items[toIndex] = result;
        items.RemoveAt(fromIndex);
        OnBankChanged?.Invoke();
        return true;
    }

    // Disassembler: a complete item at index becomes its two source components. Net +1
    // item, so it needs one free slot.
    public bool DisassembleAt(int index)
    {
        ItemComponentData item = GetAt(index);
        if (item == null || !item.isCompleteItem || item.recipeComponentA == null || item.recipeComponentB == null) return false;
        if (items.Count + 1 > capacity) return false;

        items[index] = item.recipeComponentA;
        items.Insert(index + 1, item.recipeComponentB);
        OnBankChanged?.Invoke();
        return true;
    }

    // Delirium: swaps the item at index for another (same slot count).
    public bool ReplaceAt(int index, ItemComponentData newItem)
    {
        if (newItem == null || index < 0 || index >= items.Count) return false;

        items[index] = newItem;
        OnBankChanged?.Invoke();
        return true;
    }

    // Reorders the bank: dropping onto an occupied slot swaps the two items, dropping
    // onto an empty slot sends the item to the end of the list
    public bool Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Count || fromIndex == toIndex) return false;

        if (toIndex >= 0 && toIndex < items.Count)
        {
            (items[fromIndex], items[toIndex]) = (items[toIndex], items[fromIndex]);
        }
        else
        {
            ItemComponentData moved = items[fromIndex];
            items.RemoveAt(fromIndex);
            items.Add(moved);
        }

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