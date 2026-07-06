using System;
using UnityEngine;

// Minimal stand-in for a real economy system. Replace/extend once shop, battle rewards, etc. exist.
public class PlayerGold : MonoBehaviour
{
    public int startingGold = 0;

    public int Current { get; private set; }

    public event Action<int> OnGoldChanged;

    void Awake()
    {
        Current = startingGold;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0 || Current < amount) return false;

        Current -= amount;
        OnGoldChanged?.Invoke(Current);
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0) return;

        Current += amount;
        OnGoldChanged?.Invoke(Current);
    }
}