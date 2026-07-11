using System.Collections.Generic;

// Flat modifiers are summed onto the base stats first. Percent modifiers are then summed per
// stat and applied once on top of that flat total - e.g. two +10% DEF components give +20% DEF
// total, not +10% compounded twice.
public static class StatModifierApplier
{
    public static CharacterStats Apply(CharacterStats baseStats, IList<ItemComponentData> equippedItems)
    {
        foreach (ItemComponentData item in equippedItems)
        {
            if (item == null) continue;
            foreach (StatModifier modifier in item.modifiers)
            {
                if (modifier.modifierType == ModifierType.Flat)
                    baseStats.Add(modifier.statType, modifier.value);
            }
        }

        Dictionary<StatType, float> percentTotals = new Dictionary<StatType, float>();
        foreach (ItemComponentData item in equippedItems)
        {
            if (item == null) continue;
            foreach (StatModifier modifier in item.modifiers)
            {
                if (modifier.modifierType != ModifierType.PercentOfBase) continue;
                percentTotals.TryGetValue(modifier.statType, out float current);
                percentTotals[modifier.statType] = current + modifier.value;
            }
        }

        foreach (KeyValuePair<StatType, float> pair in percentTotals)
        {
            float currentValue = baseStats.Get(pair.Key);
            baseStats.Add(pair.Key, currentValue * pair.Value / 100f);
        }

        return baseStats;
    }
}