using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks which components are equipped on each champion, keyed by CharacterData just like
// PlayerRoster keys levels/mementos - so a champion keeps its equipped items whether it's
// currently sitting on the bench or deployed in the active party.
public class CharacterEquipmentManager : MonoBehaviour
{
    public const int SlotsPerCharacter = 3;

    public PlayerRoster roster;
    public PlayerItemBank bank;

    readonly Dictionary<CharacterData, ItemComponentData[]> equipped = new Dictionary<CharacterData, ItemComponentData[]>();

    public event Action<CharacterData> OnEquipmentChanged;
    public event Action<string> OnActionFailed;

    public ItemComponentData GetEquippedAt(CharacterData champion, int slotIndex)
    {
        if (champion == null || !IsValidSlot(slotIndex)) return null;
        return SlotsFor(champion)[slotIndex];
    }

    // Pulls a component out of the bank and into a champion's slot. If that slot already held
    // something, the displaced item goes back to the bank slot just freed - this never needs
    // spare bank space to complete.
    public bool Equip(CharacterData champion, int bankIndex, int slotIndex)
    {
        if (!CanEquip(champion, slotIndex)) return false;

        ItemComponentData incoming = bank.GetAt(bankIndex);
        if (incoming == null)
        {
            OnActionFailed?.Invoke("No item at that bank slot.");
            return false;
        }

        ItemComponentData[] slots = SlotsFor(champion);
        ItemComponentData outgoing = slots[slotIndex];

        bank.RemoveAt(bankIndex, out _);
        slots[slotIndex] = incoming;
        if (outgoing != null) bank.Add(outgoing);

        OnEquipmentChanged?.Invoke(champion);
        return true;
    }

    // Sends an equipped component back to the bank. Fails if the bank has no room, per REGRAS'
    // "no space in the bank" rule.
    public bool Unequip(CharacterData champion, int slotIndex)
    {
        if (champion == null || !IsValidSlot(slotIndex))
        {
            OnActionFailed?.Invoke("Invalid equipment slot.");
            return false;
        }

        ItemComponentData[] slots = SlotsFor(champion);
        ItemComponentData item = slots[slotIndex];
        if (item == null)
        {
            OnActionFailed?.Invoke("That slot is already empty.");
            return false;
        }

        if (bank.IsFull)
        {
            OnActionFailed?.Invoke("Item bank is full.");
            return false;
        }

        slots[slotIndex] = null;
        bank.Add(item);

        OnEquipmentChanged?.Invoke(champion);
        return true;
    }

    // Swaps two equipped components directly between two champions' slots - no bank space needed
    // since nothing ever leaves equipment for the bank in this path.
    public bool SwapBetweenCharacters(CharacterData championA, int slotA, CharacterData championB, int slotB)
    {
        if (!CanEquip(championA, slotA) || !CanEquip(championB, slotB)) return false;

        ItemComponentData[] slotsA = SlotsFor(championA);
        ItemComponentData[] slotsB = SlotsFor(championB);

        ItemComponentData temp = slotsA[slotA];
        slotsA[slotA] = slotsB[slotB];
        slotsB[slotB] = temp;

        OnEquipmentChanged?.Invoke(championA);
        if (championB != championA) OnEquipmentChanged?.Invoke(championB);
        return true;
    }

    // Full pipeline: base CharacterData -> level-up bonus (via PlayerRoster's level) -> equipment modifiers.
    public CharacterStats GetEffectiveStats(CharacterData champion)
    {
        if (champion == null) return new CharacterStats();

        CharacterStats stats = CharacterStats.FromBase(champion);
        int level = roster != null ? Mathf.Max(roster.GetLevel(champion), 1) : 1;
        LevelUpCalculator.ApplyLevelBonus(stats, champion, level);

        return StatModifierApplier.Apply(stats, SlotsFor(champion));
    }

    bool CanEquip(CharacterData champion, int slotIndex)
    {
        if (champion == null || !IsValidSlot(slotIndex))
        {
            OnActionFailed?.Invoke("Invalid equipment slot.");
            return false;
        }

        if (roster == null || !roster.Owns(champion))
        {
            OnActionFailed?.Invoke("Champion is not owned by the player.");
            return false;
        }

        return true;
    }

    bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < SlotsPerCharacter;

    ItemComponentData[] SlotsFor(CharacterData champion)
    {
        if (!equipped.TryGetValue(champion, out ItemComponentData[] slots))
        {
            slots = new ItemComponentData[SlotsPerCharacter];
            equipped[champion] = slots;
        }
        return slots;
    }
}