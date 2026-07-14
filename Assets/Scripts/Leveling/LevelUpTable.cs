using System.Collections.Generic;

// Stat gains applied ONCE PER LEVEL, taken directly from REGRAS.docx's "TABELA DE LEVEL-UP".
// Treat the returned CharacterStats as read-only - it's the shared table entry, not a copy.
public static class LevelUpTable
{
    static readonly Dictionary<CharacterClass, CharacterStats> bonusPerLevel = new Dictionary<CharacterClass, CharacterStats>
    {
        { CharacterClass.Tank, new CharacterStats { maxHP = 150f, AD = 2f, AP = 1f, attackSpeed = 0.01f, critChance = 0f, DEF = 5f, MDEF = 4f, manaPerSecond = 0f } },
        { CharacterClass.OffTank, new CharacterStats { maxHP = 120f, AD = 4f, AP = 3f, attackSpeed = 0.015f, critChance = 0f, DEF = 4f, MDEF = 3f, manaPerSecond = 0f } },
        { CharacterClass.DPS, new CharacterStats { maxHP = 60f, AD = 8f, AP = 1f, attackSpeed = 0.03f, critChance = 0.005f, DEF = 1f, MDEF = 1f, manaPerSecond = 0f } },
        { CharacterClass.AP, new CharacterStats { maxHP = 70f, AD = 1f, AP = 10f, attackSpeed = 0.01f, critChance = 0f, DEF = 1f, MDEF = 3f, manaPerSecond = 0.2f } },
        { CharacterClass.Support, new CharacterStats { maxHP = 100f, AD = 1f, AP = 7f, attackSpeed = 0.02f, critChance = 0f, DEF = 3f, MDEF = 4f, manaPerSecond = 0.3f } },
        { CharacterClass.YuriaSpecial, new CharacterStats { maxHP = 80f, AD = 10f, AP = 10f, attackSpeed = 0.04f, critChance = 0.01f, DEF = 1f, MDEF = 1f, manaPerSecond = 0f } },
    };

    public static CharacterStats GetBonusFor(CharacterClass characterClass)
    {
        return bonusPerLevel.TryGetValue(characterClass, out CharacterStats bonus) ? bonus : new CharacterStats();
    }
}