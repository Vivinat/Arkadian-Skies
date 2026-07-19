using System.Collections.Generic;
using UnityEngine;

// Executes the map consumable effects against the bank / roster / party.
public class OtherItemManager : MonoBehaviour
{
    public PlayerItemBank itemBank;
    public PlayerRoster roster;
    public PlayerParty party;
    public ChampionPool championPool;
    public DialogueUIController dialogueUI;

    List<ItemComponentData> CompleteItems => itemBank != null && itemBank.recipeBook != null
        ? itemBank.recipeBook.completeItems
        : null;

    public bool CanDisassemble(ItemComponentData item) => item != null && item.isCompleteItem;
    public bool CanDelirium(ItemComponentData item) => item != null && item.isCompleteItem;

    public bool Disassemble(int bankIndex) => itemBank != null && itemBank.DisassembleAt(bankIndex);

    // Swaps a complete item for a different random complete item
    public bool Delirium(int bankIndex)
    {
        ItemComponentData current = itemBank != null ? itemBank.GetAt(bankIndex) : null;
        if (current == null || !current.isCompleteItem || CompleteItems == null) return false;

        List<ItemComponentData> pool = new List<ItemComponentData>();
        foreach (ItemComponentData item in CompleteItems)
            if (item != null && item != current) pool.Add(item);
        if (pool.Count == 0) return false;

        return itemBank.ReplaceAt(bankIndex, pool[Random.Range(0, pool.Count)]);
    }

    // Artisan's Aspect: 5 random complete items to choose from
    public List<ItemComponentData> RollArtisanChoices(int count)
    {
        List<ItemComponentData> result = new List<ItemComponentData>();
        if (CompleteItems == null || CompleteItems.Count == 0) return result;

        List<ItemComponentData> pool = new List<ItemComponentData>(CompleteItems);
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int pick = Random.Range(0, pool.Count);
            result.Add(pool[pick]);
            pool.RemoveAt(pick);
        }
        return result;
    }

    public bool GrantItem(ItemComponentData item) => itemBank != null && itemBank.Add(item);

    // Nostalgia: replaces a champion with a random one of the same cost (rarity). A brand
    // new champion arrives at level 1; if it's one you already own, it instead becomes a
    // memento (progress toward that champion's next level up).
    public bool Nostalgia(ChampionSlotRef slot)
    {
        CharacterData old = GetChampionAt(slot);
        if (old == null || championPool == null || roster == null) return false;

        List<CharacterData> candidates = championPool.GetByRarity(old.rarity);
        candidates.RemoveAll(c => c == old);
        if (candidates.Count == 0) return false;

        CharacterData rolled = candidates[Random.Range(0, candidates.Count)];

        bool rolledOwned = roster.Owns(rolled);
        bool slotIsBench = slot.container == SlotContainer.Bench;
        // a new champion needs a bench slot; a bench-sourced swap frees one, a party-sourced one may not
        if (!rolledOwned && !slotIsBench && roster.IsBenchFull) return false;

        SetChampionAt(slot, null);
        roster.ReleaseOwnership(old);

        if (rolledOwned)
        {
            bool leveled = roster.AddMemento(rolled);
            if (leveled && dialogueUI != null) dialogueUI.Show(rolled, ChampionQuoteType.LevelUp);
        }
        else
        {
            roster.AddToBench(rolled);
            if (dialogueUI != null) dialogueUI.Show(rolled, ChampionQuoteType.Select);
        }
        return true;
    }

    CharacterData GetChampionAt(ChampionSlotRef slot)
    {
        return slot.container == SlotContainer.Bench
            ? roster.GetBenchAt(slot.index)
            : party.GetChampionAt(slot.position, slot.index);
    }

    void SetChampionAt(ChampionSlotRef slot, CharacterData champion)
    {
        if (slot.container == SlotContainer.Bench) roster.SetBenchAt(slot.index, champion);
        else party.SetChampionAt(slot.position, slot.index, champion);
    }
}
