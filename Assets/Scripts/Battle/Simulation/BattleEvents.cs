using System;

public class BattleEvents
{
    public event Action<BattleUnit, BattleUnit> OnAutoAttack;
    public event Action<BattleUnit, BattleUnit> OnKill;
    // caster, the ability that was used, optional display label for the UI (null = use the ability's name)
    public event Action<BattleUnit, Ability, string> OnAbilityUsed;
    public event Action<BattleUnit, BattleUnit> OnBeforeSingleTargetAbility;

    // fired for ANY unit (ally or enemy) that gets stunned; listeners filter by side themselves
    public event Action<BattleUnit> OnUnitStunned;

    // fired whenever a unit permanently changes grid position mid-battle (e.g. Nikkal's Elder's Repositioning)
    public event Action OnPositionsChanged;

    // fired for ANY unit right after it takes damage, with the final (post-interception) amount.
    // Used for passives that react to being hit rather than blocking the hit (e.g. Luna's Papillon).
    public event Action<BattleUnit, BattleUnit, float, DamageType> OnDamageTaken;

    public void RaiseAutoAttack(BattleUnit attacker, BattleUnit target)
    {
        OnAutoAttack?.Invoke(attacker, target);
    }

    public void RaiseKill(BattleUnit killer, BattleUnit victim)
    {
        OnKill?.Invoke(killer, victim);
    }

    public void RaiseAbilityUsed(BattleUnit caster, Ability ability, string displayLabel = null)
    {
        OnAbilityUsed?.Invoke(caster, ability, displayLabel);
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

    public void RaiseDamageTaken(BattleUnit victim, BattleUnit attacker, float damage, DamageType damageType)
    {
        OnDamageTaken?.Invoke(victim, attacker, damage, damageType);
    }
}