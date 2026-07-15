using UnityEngine;

// REGRAS.docx: auto-attacks crit naturally; a crit deals 150% damage, boosted further by
// CritDamage from items. Abilities only crit if equipped with an item that enables it (not yet
// in the codebase) - so only auto-attack-shaped damage should call this, not the generic
// DamageEffect ability pipeline.
public static class CritCalculator
{
    const float BaseCritMultiplier = 1.5f;

    public static bool Roll(float critChance)
    {
        return Random.value < critChance;
    }

    public static float GetMultiplier(float critDamageBonusPercent)
    {
        return BaseCritMultiplier + (critDamageBonusPercent / 100f);
    }
}
