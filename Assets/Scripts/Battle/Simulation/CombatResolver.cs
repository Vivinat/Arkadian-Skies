using UnityEngine;

public static class CombatResolver
{
    // defensePenetrationPercent (0-1): ignores that fraction of the target's defense before mitigation,
    // e.g. Luna's Papillon ignoring 50% of the target's DEF.
    public static void ApplyDamage(BattleUnit attacker, BattleUnit target, float rawDamage, DamageType damageType, BattleContext context, bool allowInterception = true, float defensePenetrationPercent = 0f)
    {
        if (!target.IsAlive) return;

        float baseDefense = damageType == DamageType.AP ? target.EffectiveMDEF : target.EffectiveDEF;
        float defense = baseDefense * (1f - Mathf.Clamp01(defensePenetrationPercent));
        float damage = DamageCalculator.CalculateFinalDamage(rawDamage, defense, damageType);

        if (allowInterception && TryGetInterceptor(target, out IDamageInterceptor interceptor))
        {
            damage = interceptor.OnBeforeDamage(target, attacker, damage, damageType, context, out bool triggered);
            if (triggered && target.EffectiveManaPerSecond > 0f) target.currentMana = 0f;
        }

        target.TakeDamage(damage);
        Debug.Log($"{BattleLog.LabelOf(attacker)} deals {damage:F1} {damageType} damage to {BattleLog.LabelOf(target)}. {BattleLog.LabelOf(target)} HP: {target.currentHP:F1}/{target.EffectiveMaxHP}");

        context.events.RaiseDamageTaken(target, attacker, damage, damageType);

        if (!target.IsAlive)
        {
            Debug.Log($"{BattleLog.LabelOf(target)} has died.");
            context.events.RaiseKill(attacker, target);
        }
    }

    // centralizes stun application so any future ability can inflict it and reactive passives (e.g. Nikkal) get notified
    public static void ApplyStun(BattleUnit target, float duration, BattleContext context)
    {
        if (!target.IsAlive) return;

        target.SetStunned(duration);
        Debug.Log($"{BattleLog.LabelOf(target)} is stunned for {duration:F1}s.");
        context.events.RaiseUnitStunned(target);
    }

    // Severe Wounds / heal block, for a specific duration.
    public static void ApplyHealBlock(BattleUnit target, float duration, BattleContext context)
    {
        if (!target.IsAlive) return;

        target.ApplyHealBlock(duration);
        Debug.Log($"{BattleLog.LabelOf(target)} suffers Severe Wounds and cannot be healed for {duration:F1}s.");
    }

    static bool TryGetInterceptor(BattleUnit target, out IDamageInterceptor interceptor)
    {
        interceptor = null;
        Ability reactive = target.GetAbility();
        if (reactive == null || reactive.triggerType != TriggerType.OnDamageTaken) return false;
        if (!(reactive.customExecutor is IDamageInterceptor found)) return false;

        bool ready = found is IAlwaysReadyInterceptor || target.EffectiveManaPerSecond <= 0f || target.currentMana >= 100f;
        if (!ready) return false;

        interceptor = found;
        return true;
    }
}
