using System;
using UnityEngine;

// Single entry point for every move that can happen to a champion: bench <-> party, reordering
// within either one, and selling. UI code should only ever talk to this class.
public class PartyManager : MonoBehaviour
{
    public PlayerRoster roster;
    public PlayerParty party;
    public PlayerGold gold;

    public event Action<ChampionSlotRef, ChampionSlotRef> OnSlotsSwapped;
    public event Action<CharacterData, int> OnChampionSold;
    public event Action<string> OnActionFailed;

    public CharacterData GetChampionAt(ChampionSlotRef slot)
    {
        return slot.container == SlotContainer.Bench
            ? roster.GetBenchAt(slot.index)
            : party.GetChampionAt(slot.position, slot.index);
    }

    // Covers every drag-and-drop case: reordering the bench, reordering the party, and moving a
    // champion between the two. Always legal - it only ever relocates existing slots, so bench
    // (8) and party (2 Frontline + 2 Backline) capacity can never be exceeded by a swap.
    public bool SwapSlots(ChampionSlotRef from, ChampionSlotRef to)
    {
        if (!IsValidSlot(from) || !IsValidSlot(to))
        {
            OnActionFailed?.Invoke("Invalid slot.");
            return false;
        }

        if (from == to) return true;

        CharacterData champA = GetChampionAt(from);
        CharacterData champB = GetChampionAt(to);
        if (champA == null && champB == null) return true;

        SetChampionAt(from, champB);
        SetChampionAt(to, champA);

        OnSlotsSwapped?.Invoke(from, to);
        return true;
    }

    // Drag a portrait out and release it somewhere that isn't a slot -> sell it for gold.
    public bool SellChampion(ChampionSlotRef slot)
    {
        if (!IsValidSlot(slot))
        {
            OnActionFailed?.Invoke("Invalid slot.");
            return false;
        }

        CharacterData champion = GetChampionAt(slot);
        if (champion == null)
        {
            OnActionFailed?.Invoke("That slot is already empty.");
            return false;
        }

        SetChampionAt(slot, null);
        roster.ReleaseOwnership(champion);

        int value = RouletteCosts.SellValue(champion.rarity);
        if (gold != null) gold.Add(value);

        OnChampionSold?.Invoke(champion, value);
        return true;
    }

    // Convenience wrapper for non-drag callers (e.g. picking a starter champion at run start).
    public bool DeployFromBench(CharacterData champion, BattlePosition position, int slotIndex)
    {
        if (!roster.FindBenchSlot(champion, out int benchIndex))
        {
            OnActionFailed?.Invoke("Champion is not on the bench.");
            return false;
        }

        return SwapSlots(ChampionSlotRef.BenchSlot(benchIndex), ChampionSlotRef.PartySlot(position, slotIndex));
    }

    // Convenience wrapper: sends a party member back to the first free bench slot (no drag involved).
    public bool BenchFromParty(BattlePosition position, int slotIndex)
    {
        int freeSlot = roster.FindFirstEmptyBenchSlot();
        if (freeSlot < 0)
        {
            OnActionFailed?.Invoke("Bench is full.");
            return false;
        }

        return SwapSlots(ChampionSlotRef.PartySlot(position, slotIndex), ChampionSlotRef.BenchSlot(freeSlot));
    }

    // Hands the current squad to the static bridge BattleSimulator reads from when the Battle scene loads.
    public void SendPartyToBattle()
    {
        BattleSetup.allyComposition = party.ToComposition();
    }

    bool IsValidSlot(ChampionSlotRef slot)
    {
        return slot.container == SlotContainer.Bench
            ? roster.IsValidBenchSlot(slot.index)
            : party.IsValidSlot(slot.position, slot.index);
    }

    void SetChampionAt(ChampionSlotRef slot, CharacterData champion)
    {
        if (slot.container == SlotContainer.Bench) roster.SetBenchAt(slot.index, champion);
        else party.SetChampionAt(slot.position, slot.index, champion);
    }

    [ContextMenu("Log Bench And Party State")]
    void LogState()
    {
        string benchNames = "";
        for (int i = 0; i < roster.benchCapacity; i++)
        {
            CharacterData c = roster.GetBenchAt(i);
            benchNames += (c != null ? c.characterName : "-") + " ";
        }
        Debug.Log($"Bench: {benchNames}");

        LogRow(BattlePosition.Frontline);
        LogRow(BattlePosition.Backline);
    }

    void LogRow(BattlePosition position)
    {
        int length = position == BattlePosition.Frontline ? PlayerParty.FrontlineCapacity : PlayerParty.BacklineCapacity;
        for (int i = 0; i < length; i++)
        {
            CharacterData c = party.GetChampionAt(position, i);
            Debug.Log($"{position}[{i}]: {(c != null ? c.characterName : "empty")}");
        }
    }
}