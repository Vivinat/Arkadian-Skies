using System;

public class BattleEvents
{
    public event Action<BattleUnit, BattleUnit> OnAutoAttack;
    public event Action<BattleUnit, BattleUnit> OnKill;
    public event Action<BattleUnit> OnAbilityUsed;
    public event Action<BattleUnit, BattleUnit> OnBeforeSingleTargetAbility;

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

    public void RaiseBeforeSingleTargetAbility(BattleUnit caster, BattleUnit target)
    {
        OnBeforeSingleTargetAbility?.Invoke(caster, target);
    }
}