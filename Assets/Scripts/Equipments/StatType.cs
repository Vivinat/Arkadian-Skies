// Mirrors the numeric fields on CharacterData. CritDamage has no base field there (base is
// always 0 - the +50% crit convention from ITEMS_COMPLETOS lives in combat code, not here),
// it only exists so item modifiers have somewhere to add their bonus.
public enum StatType
{
    MaxHP,
    AD,
    AP,
    AttackSpeed,
    CritChance,
    CritDamage,
    DEF,
    MDEF,
    ManaPerSecond
}