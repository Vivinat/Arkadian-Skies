using System;

public class BattleEvents
{
    // fired right after an auto-attack lands, before death check
    public event Action<BattleUnit, BattleUnit> OnAutoAttack;

    // fired right after a unit dies, passing (killer, victim)
    public event Action<BattleUnit, BattleUnit> OnKill;

    // fired right after any ability (mana or otherwise) executes
    public event Action<BattleUnit> OnAbilityUsed;

    public void RaiseAutoAttack(BattleUnit attacker, BattleUnit target)
    {
        OnAutoAttack?.Invoke(attacker, target);
    }

    public void RaiseKill(BattleUnit killer, BattleUnit victim)
    {
        OnKill?.Invoke(killer, victim);
    }

    public void RaiseAbilityUsed(BattleUnit caster)
    {
        OnAbilityUsed?.Invoke(caster);
    }
}