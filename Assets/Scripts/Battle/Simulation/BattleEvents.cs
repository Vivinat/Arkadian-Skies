using System;

public class BattleEvents
{
    public event Action<BattleUnit, BattleUnit> OnAutoAttack;
    public event Action<BattleUnit, BattleUnit> OnKill;
    public event Action<BattleUnit> OnAbilityUsed;
    public event Action<BattleUnit, BattleUnit> OnBeforeSingleTargetAbility;

    // fired for ANY unit (ally or enemy) that gets stunned; listeners filter by side themselves
    public event Action<BattleUnit> OnUnitStunned;

    // fired whenever a unit permanently changes grid position mid-battle (e.g. Nikkal's Elder's Repositioning)
    public event Action OnPositionsChanged;

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

    public void RaiseUnitStunned(BattleUnit unit)
    {
        OnUnitStunned?.Invoke(unit);
    }

    public void RaisePositionsChanged()
    {
        OnPositionsChanged?.Invoke();
    }
}