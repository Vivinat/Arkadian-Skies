using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Executes global item effects on the live battle. Per the rules: only during a pause,
// and only one item per pause. Timed effects freeze while the battle is paused.
public class GlobalItemManager : MonoBehaviour
{
    public BattleSimulator simulator;
    public BattlePauseController pauseController;

    public event Action<string> OnItemUsed;
    public event Action<string> OnUseFailed;

    // Each use costs one pause charge - with 10 charges banked, 10 items can be used in
    // a single pause
    public bool Use(GlobalItemData item)
    {
        if (item == null) return false;

        if (pauseController == null || !pauseController.IsPaused)
        {
            OnUseFailed?.Invoke("Global items can only be used during a pause.");
            return false;
        }
        if (pauseController.AvailablePauses <= 0)
        {
            OnUseFailed?.Invoke("No pause charges left! 1 use = 1 pause.");
            return false;
        }

        BattleContext context = simulator.Context;
        if (context == null) return false;

        bool executed = Execute(item, context);
        if (!executed)
        {
            OnUseFailed?.Invoke($"{item.itemName} is not implemented yet.");
            return false;
        }

        pauseController.TrySpendPause();
        OnItemUsed?.Invoke(item.itemName);
        Debug.Log($"Global item used: {item.itemName}");
        return true;
    }

    bool Execute(GlobalItemData item, BattleContext context)
    {
        switch (item.effectType)
        {
            case GlobalItemEffectType.DamageAllEnemies:
                DamageAllEnemies(item.power, context);
                return true;

            case GlobalItemEffectType.HealAllAllies:
                foreach (BattleUnit ally in context.allySide.GetAllAlive()) ally.Heal(item.power);
                return true;

            case GlobalItemEffectType.HealAndRegenAllies:
                foreach (BattleUnit ally in context.allySide.GetAllAlive()) ally.Heal(ally.EffectiveMaxHP * 0.30f);
                StartCoroutine(RegenRoutine(context, item.power, item.duration));
                return true;

            case GlobalItemEffectType.WoundsAndBleedEnemies:
                foreach (BattleUnit enemy in context.enemySide.GetAllAlive())
                    CombatResolver.ApplyHealBlock(enemy, item.duration, context);
                StartCoroutine(BleedRoutine(context, item.power, item.duration));
                return true;

            case GlobalItemEffectType.ReviveFallenAllies:
                ReviveFallenAllies(context);
                return true;

            case GlobalItemEffectType.TeamLifesteal:
                StartCoroutine(LifestealRoutine(context, item.power, item.duration));
                return true;

            case GlobalItemEffectType.ReflectDamage:
                StartCoroutine(ReflectRoutine(context, item.power, item.duration));
                return true;

            default:
                return false; // DoubleTeamHP and BlindAndSilence need systems that don't exist yet
        }
    }

    void DamageAllEnemies(float damage, BattleContext context)
    {
        // nominal attacker so logs and kill credit stay coherent
        List<BattleUnit> allies = context.allySide.GetAllAlive();
        BattleUnit attacker = allies.Count > 0 ? allies[0] : null;
        if (attacker == null) return;

        foreach (BattleUnit enemy in context.enemySide.GetAllAlive())
            CombatResolver.ApplyDamage(attacker, enemy, damage, DamageType.AD, context);
    }

    void ReviveFallenAllies(BattleContext context)
    {
        bool revivedAny = false;
        foreach (BattleUnit unit in context.allySide.GetAllUnits())
        {
            if (unit.IsAlive) continue;

            unit.currentHP = unit.EffectiveMaxHP * 0.40f;
            unit.currentMana = 100f;
            revivedAny = true;
            Debug.Log($"{BattleLog.LabelOf(unit)} is revived by The Necromancer's Call.");
        }

        // rebinds every bar/portrait so revived units come back on screen
        if (revivedAny) context.events.RaisePositionsChanged();
    }

    IEnumerator RegenRoutine(BattleContext context, float percentPerSecond, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            yield return BattleSecond(1f);
            t += 1f;
            foreach (BattleUnit ally in context.allySide.GetAllAlive())
                ally.Heal(ally.EffectiveMaxHP * percentPerSecond);
        }
    }

    IEnumerator BleedRoutine(BattleContext context, float percentPerSecond, float duration)
    {
        List<BattleUnit> bleeding = context.enemySide.GetAllAlive();
        float t = 0f;
        while (t < duration)
        {
            yield return BattleSecond(1f);
            t += 1f;
            foreach (BattleUnit enemy in bleeding)
            {
                if (!enemy.IsAlive) continue;
                enemy.TakeDamage(enemy.EffectiveMaxHP * percentPerSecond);
            }
        }
    }

    IEnumerator LifestealRoutine(BattleContext context, float percent, float duration)
    {
        // lifesteal only applies to AD damage, per the rules
        Action<BattleUnit, BattleUnit, float, DamageType> handler = (victim, attacker, damage, type) =>
        {
            if (attacker != null && attacker.IsAlive && attacker.side == BattleSide.Ally && type == DamageType.AD)
                attacker.Heal(damage * percent);
        };

        context.events.OnDamageTaken += handler;
        yield return BattleSecond(duration);
        context.events.OnDamageTaken -= handler;
    }

    IEnumerator ReflectRoutine(BattleContext context, float percent, float duration)
    {
        Action<BattleUnit, BattleUnit, float, DamageType> handler = (victim, attacker, damage, type) =>
        {
            if (victim.side != BattleSide.Ally || attacker == null || !attacker.IsAlive) return;
            if (type != DamageType.AD && type != DamageType.AP) return;
            CombatResolver.ApplyDamage(victim, attacker, damage * percent, DamageType.AP, context, allowInterception: false);
        };

        context.events.OnDamageTaken += handler;
        yield return BattleSecond(duration);
        context.events.OnDamageTaken -= handler;
    }

    // Waits in battle time: the clock stops while the fight is paused
    IEnumerator BattleSecond(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (pauseController == null || !pauseController.IsPaused) t += Time.deltaTime;
            yield return null;
        }
    }
}
