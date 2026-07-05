using UnityEngine;

public static class CombatResolver
{
    public static void ApplyDamage(BattleUnit attacker, BattleUnit target, float rawDamage, DamageType damageType, BattleContext context, bool allowInterception = true)
    {
        if (!target.IsAlive) return;

        float defense = damageType == DamageType.AP ? target.EffectiveMDEF : target.EffectiveDEF;
        float damage = DamageCalculator.CalculateFinalDamage(rawDamage, defense, damageType);

        if (allowInterception && TryGetInterceptor(target, out IDamageInterceptor interceptor))
        {
            damage = interceptor.OnBeforeDamage(target, attacker, damage, damageType, context, out bool triggered);
            if (triggered && target.data.manaPerSecond > 0f) target.currentMana = 0f;
        }

        target.TakeDamage(damage);
        Debug.Log($"{BattleLog.LabelOf(attacker)} deals {damage:F1} {damageType} damage to {BattleLog.LabelOf(target)}. {BattleLog.LabelOf(target)} HP: {target.currentHP:F1}/{target.data.maxHP}");

        if (!target.IsAlive)
        {
            Debug.Log($"{BattleLog.LabelOf(target)} has died.");
            context.events.RaiseKill(attacker, target);
        }
    }

    static bool TryGetInterceptor(BattleUnit target, out IDamageInterceptor interceptor)
    {
        interceptor = null;
        Ability reactive = target.GetAbility();
        if (reactive == null || reactive.triggerType != TriggerType.OnDamageTaken) return false;
        if (!(reactive.customExecutor is IDamageInterceptor found)) return false;

        bool ready = target.data.manaPerSecond <= 0f || target.currentMana >= 100f;
        if (!ready) return false;

        interceptor = found;
        return true;
    }
}