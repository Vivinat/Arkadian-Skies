using System;
using System.Collections.Generic;
using UnityEngine;

// Holds the player's active squad: up to 2 Frontline + 2 Backline champions.
// Only stores which CharacterData sits in each slot - BattleUnit instances are created later,
// by BattleSimulator, from the SlotAssignment list ToComposition() builds.
public class PlayerParty : MonoBehaviour
{
    public const int FrontlineCapacity = 2;
    public const int BacklineCapacity = 2;
    public const int MaxPartySize = FrontlineCapacity + BacklineCapacity;

    readonly CharacterData[] frontline = new CharacterData[FrontlineCapacity];
    readonly CharacterData[] backline = new CharacterData[BacklineCapacity];

    public event Action OnChanged;

    public bool IsValidSlot(BattlePosition position, int slotIndex)
    {
        int length = position == BattlePosition.Frontline ? FrontlineCapacity : BacklineCapacity;
        return slotIndex >= 0 && slotIndex < length;
    }

    public CharacterData GetChampionAt(BattlePosition position, int slotIndex)
    {
        return IsValidSlot(position, slotIndex) ? ArrayFor(position)[slotIndex] : null;
    }

    // Low-level slot write. Prefer PartyManager for anything player-facing -
    // it's the one that keeps this in sync with the bench.
    public void SetChampionAt(BattlePosition position, int slotIndex, CharacterData champion)
    {
        if (!IsValidSlot(position, slotIndex)) return;
        ArrayFor(position)[slotIndex] = champion;
        OnChanged?.Invoke();
    }

    public bool Contains(CharacterData champion) => FindSlot(champion, out _, out _);

    public bool FindSlot(CharacterData champion, out BattlePosition position, out int slotIndex)
    {
        if (FindInArray(frontline, champion, out slotIndex)) { position = BattlePosition.Frontline; return true; }
        if (FindInArray(backline, champion, out slotIndex)) { position = BattlePosition.Backline; return true; }

        position = default(BattlePosition);
        slotIndex = -1;
        return false;
    }

    public int Count()
    {
        int count = 0;
        foreach (CharacterData c in frontline) if (c != null) count++;
        foreach (CharacterData c in backline) if (c != null) count++;
        return count;
    }

    public bool IsFull => Count() >= MaxPartySize;

    // Converts the current squad into the format BattleSetup/BattleSimulator already expect.
    public List<SlotAssignment> ToComposition()
    {
        List<SlotAssignment> result = new List<SlotAssignment>();
        CollectAssignments(result, frontline, BattlePosition.Frontline);
        CollectAssignments(result, backline, BattlePosition.Backline);
        return result;
    }

    CharacterData[] ArrayFor(BattlePosition position)
    {
        return position == BattlePosition.Frontline ? frontline : backline;
    }

    static bool FindInArray(CharacterData[] array, CharacterData champion, out int index)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == champion) { index = i; return true; }
        }
        index = -1;
        return false;
    }

    void CollectAssignments(List<SlotAssignment> result, CharacterData[] array, BattlePosition position)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == null) continue;
            result.Add(new SlotAssignment { character = array[i], position = position, slotIndex = i });
        }
    }
}