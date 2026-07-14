using System;
using UnityEngine;

// Turns a champion's base CharacterData + current level into its actual current stats.
// PlayerRoster already tracks the level number (3 mementos = level up); this is the missing
// piece that turns that number into real stat gains, matching REGRAS.docx's level-up table.
public static class LevelUpCalculator
{
    public static CharacterStats GetStatsAtLevel(CharacterData data, int level)
    {
        CharacterStats stats = CharacterStats.FromBase(data);
        ApplyLevelBonus(stats, data, level);
        return stats;
    }

    // Adds the level-up bonus directly onto an existing stats snapshot, so it can be stacked with
    // other bonus sources (e.g. CharacterEquipmentManager applying equipment on the same object).
    public static void ApplyLevelBonus(CharacterStats stats, CharacterData data, int level)
    {
        int levelsGained = Mathf.Max(level - 1, 0);
        if (levelsGained <= 0) return;

        CharacterStats bonusPerLevel = LevelUpTable.GetBonusFor(data.characterClass);

        // REGRAS.docx: Mana/s gains only apply to characters that actually use Mana as their resource.
        bool usesMana = data.manaPerSecond > 0f;

        foreach (StatType statType in (StatType[])Enum.GetValues(typeof(StatType)))
        {
            if (statType == StatType.ManaPerSecond && !usesMana) continue;
            stats.Add(statType, bonusPerLevel.Get(statType) * levelsGained);
        }
    }
}