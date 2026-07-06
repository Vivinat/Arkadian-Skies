using System.Collections.Generic;
using UnityEngine;

// Minimal stand-in for the real bench/party system. Tracks who the player owns, how many
// mementos of each champion have piled up, and their current level.
public class PlayerRoster : MonoBehaviour
{
    public int benchCapacity = 8;
    public int mementosToLevelUp = 3;

    readonly List<CharacterData> bench = new List<CharacterData>();
    readonly Dictionary<CharacterData, int> mementoCounts = new Dictionary<CharacterData, int>();
    readonly Dictionary<CharacterData, int> levels = new Dictionary<CharacterData, int>();

    public bool IsBenchFull => bench.Count >= benchCapacity;

    public bool Owns(CharacterData champion) => bench.Contains(champion);

    public void AddToBench(CharacterData champion)
    {
        if (IsBenchFull) return;

        bench.Add(champion);
        levels[champion] = 1;
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