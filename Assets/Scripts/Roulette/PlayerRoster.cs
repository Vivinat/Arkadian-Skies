using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks every champion the player has ever acquired (for memento/level bookkeeping) and holds
// the bench as fixed, indexed slots so champions can be freely reordered within it, not just
// added or removed. A champion stops being "on the bench" once it's deployed to the active party,
// but stays owned - ownership only ends when the champion is sold (see ReleaseOwnership).
public class PlayerRoster : MonoBehaviour
{
    public int benchCapacity = 8;
    public int mementosToLevelUp = 3;

    readonly HashSet<CharacterData> ownedChampions = new HashSet<CharacterData>();
    readonly Dictionary<CharacterData, int> mementoCounts = new Dictionary<CharacterData, int>();
    readonly Dictionary<CharacterData, int> levels = new Dictionary<CharacterData, int>();

    CharacterData[] bench;
    CharacterData[] Bench => bench ?? (bench = new CharacterData[benchCapacity]);

    // Fires on every bench mutation, no matter who caused it (Roulette purchase, PartyManager
    // swap/sell, a future starter-pick screen...) - UI should listen to this instead of trying
    // to subscribe to every possible source individually.
    public event Action OnBenchChanged;

    public IReadOnlyList<CharacterData> BenchSlots => Bench;

    public bool IsValidBenchSlot(int slotIndex) => slotIndex >= 0 && slotIndex < Bench.Length;

    public CharacterData GetBenchAt(int slotIndex) => IsValidBenchSlot(slotIndex) ? Bench[slotIndex] : null;

    public void SetBenchAt(int slotIndex, CharacterData champion)
    {
        if (!IsValidBenchSlot(slotIndex)) return;
        Bench[slotIndex] = champion;
        OnBenchChanged?.Invoke();
    }

    public int FindFirstEmptyBenchSlot()
    {
        for (int i = 0; i < Bench.Length; i++)
            if (Bench[i] == null) return i;
        return -1;
    }

    public bool FindBenchSlot(CharacterData champion, out int slotIndex)
    {
        slotIndex = champion != null ? Array.IndexOf(Bench, champion) : -1;
        return slotIndex >= 0;
    }

    public bool IsOnBench(CharacterData champion) => FindBenchSlot(champion, out _);

    public bool IsBenchFull => FindFirstEmptyBenchSlot() < 0;

    public bool Owns(CharacterData champion) => champion != null && ownedChampions.Contains(champion);

    // Places a newly (or re-)acquired champion into the first free bench slot.
    public bool AddToBench(CharacterData champion)
    {
        if (champion == null || IsOnBench(champion)) return false;

        int slot = FindFirstEmptyBenchSlot();
        if (slot < 0) return false;

        ownedChampions.Add(champion);
        Bench[slot] = champion;
        if (!levels.ContainsKey(champion)) levels[champion] = 1;

        OnBenchChanged?.Invoke();
        return true;
    }

    // Selling a champion: it stops counting as owned, so a future roll of the same champion on
    // the Roulette is treated as a brand new acquisition again instead of a memento.
    public void ReleaseOwnership(CharacterData champion)
    {
        if (champion == null) return;

        ownedChampions.Remove(champion);
        mementoCounts.Remove(champion);
        levels.Remove(champion);

        if (FindBenchSlot(champion, out int slot))
        {
            Bench[slot] = null;
            OnBenchChanged?.Invoke();
        }
    }

    public int GetLevel(CharacterData champion) => levels.TryGetValue(champion, out int level) ? level : 0;

    public int GetMementoCount(CharacterData champion) => mementoCounts.TryGetValue(champion, out int count) ? count : 0;

    // Returns true if this memento was the one that triggered a level up.
    public bool AddMemento(CharacterData champion)
    {
        int count = GetMementoCount(champion) + 1;

        if (count >= mementosToLevelUp)
        {
            mementoCounts[champion] = count - mementosToLevelUp;
            levels[champion] = GetLevel(champion) + 1;
            return true;
        }

        mementoCounts[champion] = count;
        return false;
    }
}