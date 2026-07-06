using System;
using System.Collections.Generic;
using UnityEngine;

public class RouletteManager : MonoBehaviour
{
    [Header("Config")]
    public ChampionPool championPool;
    public PlayerGold playerGold;
    public PlayerRoster playerRoster;
    [Range(1, 5)] public int resultSlotCount = 5;

    public List<RouletteResult> CurrentResults { get; } = new List<RouletteResult>();

    public event Action<List<RouletteResult>> OnRouletteSpun;
    public event Action<int, RouletteResult> OnSlotAcquired;
    public event Action<CharacterData> OnChampionAcquired;
    public event Action<CharacterData, int> OnChampionLeveledUp;
    public event Action<string> OnSpinFailed;
    public event Action<string> OnAcquireFailed;

    // preferredRarity gets a 50% chance to show up per slot, the other two split the remaining 50% (25% each).
    // Tiers with no champions configured yet (e.g. Legendary right now) are excluded and their weight redistributed.
    public bool Spin(ChampionRarity preferredRarity)
    {
        if (championPool == null || championPool.availableChampions.Count == 0)
        {
            OnSpinFailed?.Invoke("Champion pool is empty.");
            return false;
        }

        int cost = RouletteCosts.SpinCost(preferredRarity);
        if (!playerGold.TrySpend(cost))
        {
            OnSpinFailed?.Invoke("Not enough gold to spin.");
            return false;
        }

        CurrentResults.Clear();
        for (int i = 0; i < resultSlotCount; i++)
        {
            ChampionRarity rolledRarity = RollRarity(preferredRarity);
            List<CharacterData> candidates = championPool.GetByRarity(rolledRarity);
            CharacterData chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            CurrentResults.Add(new RouletteResult(chosen, rolledRarity));
        }

        OnRouletteSpun?.Invoke(CurrentResults);
        return true;
    }

    // Claims one of the 5 slots from the last spin. New champion -> goes to the bench.
    // Champion already owned -> counts as a memento towards a level up.
    public bool Acquire(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= CurrentResults.Count) return false;

        RouletteResult result = CurrentResults[slotIndex];
        if (result.claimed) return false;

        bool alreadyOwned = playerRoster.Owns(result.champion);

        if (!alreadyOwned && playerRoster.IsBenchFull)
        {
            OnAcquireFailed?.Invoke("Bench is full.");
            return false;
        }

        int cost = RouletteCosts.AcquireCost(result.rarity);
        if (!playerGold.TrySpend(cost))
        {
            OnAcquireFailed?.Invoke("Not enough gold to acquire.");
            return false;
        }

        result.claimed = true;

        if (!alreadyOwned)
        {
            playerRoster.AddToBench(result.champion);
            OnChampionAcquired?.Invoke(result.champion);
        }
        else if (playerRoster.AddMemento(result.champion))
        {
            OnChampionLeveledUp?.Invoke(result.champion, playerRoster.GetLevel(result.champion));
        }

        OnSlotAcquired?.Invoke(slotIndex, result);
        return true;
    }

    ChampionRarity RollRarity(ChampionRarity preferred)
    {
        float commonWeight = RarityWeight(ChampionRarity.Common, preferred);
        float epicWeight = RarityWeight(ChampionRarity.Epic, preferred);
        float legendaryWeight = RarityWeight(ChampionRarity.Legendary, preferred);

        float total = commonWeight + epicWeight + legendaryWeight;
        float roll = UnityEngine.Random.value * total;

        if (roll < commonWeight) return ChampionRarity.Common;
        if (roll < commonWeight + epicWeight) return ChampionRarity.Epic;
        return ChampionRarity.Legendary;
    }

    float RarityWeight(ChampionRarity rarity, ChampionRarity preferred)
    {
        if (championPool.GetByRarity(rarity).Count == 0) return 0f;
        return rarity == preferred ? 0.5f : 0.25f;
    }
}