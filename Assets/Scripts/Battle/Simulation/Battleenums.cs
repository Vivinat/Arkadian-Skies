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
    EveryNAutoAttacks,
    OnDamageTaken,             // reactive: fires when this unit is about to take damage (e.g. Jacobo Frontline)
    OnAllySingleTargetAbility  // reactive: fires when an ally uses a single-target ability (e.g. Jacobo Backline)
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
    AllOtherEnemies,
    SameAsAutoAttack
}