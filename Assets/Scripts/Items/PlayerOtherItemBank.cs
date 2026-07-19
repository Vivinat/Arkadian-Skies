using System;
using System.Collections.Generic;
using UnityEngine;

// Holds the player's map consumables. Identical items stack; capacity limits the total.
public class PlayerOtherItemBank : MonoBehaviour
{
    class Stack
    {
        public OtherItemData item;
        public int count;
    }

    public int capacity = 8;

    [Header("Debug seeding")]
    public List<OtherItemData> debugSeedItems = new List<OtherItemData>();
    public bool debugFillOnStart = false;

    readonly List<Stack> stacks = new List<Stack>();

    public event Action OnChanged;

    public int StackCount => stacks.Count;
    public bool IsFull
    {
        get
        {
            int total = 0;
            foreach (Stack stack in stacks) total += stack.count;
            return total >= capacity;
        }
    }

    void Start()
    {
        if (!debugFillOnStart) return;
        foreach (OtherItemData item in debugSeedItems) Add(item);
    }

    public OtherItemData GetItemAt(int index) => index >= 0 && index < stacks.Count ? stacks[index].item : null;
    public int GetCountAt(int index) => index >= 0 && index < stacks.Count ? stacks[index].count : 0;

    public bool Add(OtherItemData item)
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

    public bool ConsumeAt(int index)
    {
        if (index < 0 || index >= stacks.Count) return false;

        stacks[index].count--;
        if (stacks[index].count <= 0) stacks.RemoveAt(index);
        OnChanged?.Invoke();
        return true;
    }
}
