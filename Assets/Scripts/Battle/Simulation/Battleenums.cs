public enum BattlePosition
{
    Frontline,
    Backline
}

public enum BattleSide
{
    Ally,
    Enemy
}

public enum TriggerType
{
    Mana,
    OnKill,
    EveryNAutoAttacks
}

public enum DamageType
{
    AD,
    AP,
    True
}

public enum TargetFilterType
{
    LowestHPEnemy,
    HighestHPEnemy,
    AllEnemies,
    Self,
    AllOtherEnemies, // used for on-kill effects: all enemies except the one that just died
    SameAsAutoAttack // used by EveryNAutoAttacks abilities that hit whatever the normal auto-attack was targeting
}