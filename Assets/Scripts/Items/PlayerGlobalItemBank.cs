using System;
using System.Collections.Generic;
using UnityEngine;

// Holds the player's global items. Identical items stack into one entry; the capacity
// limits the TOTAL number of items counting stack sizes (max 8 per the rules).
public class PlayerGlobalItemBank : MonoBehaviour
{
    class Stack
    {
        public GlobalItemData item;
        public int count;
    }

    public int capacity = 8;

    [Header("Debug (no shop/events yet - use this to seed items for testing)")]
    public List<GlobalItemData> debugSeedItems = new List<GlobalItemData>();
    public bool debugFillOnStart = false;

    readonly List<Stack> stacks = new List<Stack>();

    public event Action OnChanged;

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (Stack stack in stacks) total += stack.count;
            return total;
        }
    }

    public bool IsFull => TotalCount >= capacity;
    public int StackCount => stacks.Count;

    void Start()
    {
        if (!debugFillOnStart) return;
        foreach (GlobalItemData item in debugSeedItems) Add(item);
    }

    public GlobalItemData GetItemAt(int stackIndex)
    {
        return stackIndex >= 0 && stackIndex < stacks.Count ? stacks[stackIndex].item : null;
    }

    public int GetCountAt(int stackIndex)
    {
        return stackIndex >= 0 && stackIndex < stacks.Count ? stacks[stackIndex].count : 0;
    }

    public bool Add(GlobalItemData item)
    {
        if (item == null || IsFull) return false;

        foreach (Stack stack in stacks)
        {
            if (stack.item != item) continue;
            stack.count++;
            OnChanged?.Invoke();
            return true;
        }

        stacks.Add(new Stack { item = item, count = 1 });
        OnChanged?.Invoke();
        return true;
    }

    public bool ConsumeAt(int stackIndex)
    {
        if (stackIndex < 0 || stackIndex >= stacks.Count) return false;

        stacks[stackIndex].count--;
        if (stacks[stackIndex].count <= 0) stacks.RemoveAt(stackIndex);
        OnChanged?.Invoke();
        return true;
    }
}
