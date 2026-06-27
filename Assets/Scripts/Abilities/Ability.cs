using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewAbility", menuName = "Autobattler/Ability")]
public class Ability : ScriptableObject
{
    public string abilityName;
    public Sprite icon;

    [Header("Trigger")]
    public TriggerType triggerType = TriggerType.Mana;

    [Tooltip("Used only when triggerType is EveryNAutoAttacks (e.g. 3 for 'every 3rd auto-attack').")]
    public int autoAttackInterval = 3;

    [Header("Targeting (used by the modular effect list, ignored by customExecutor)")]
    public TargetFilterType targetFilter = TargetFilterType.LowestHPEnemy;

    [Header("Modular Effects (used if customExecutor is empty)")]
    public List<BaseEffect> effects = new List<BaseEffect>();

    [Header("Custom Override (bypasses targetFilter and effects entirely)")]
    public AbilityOverrideSO customExecutor;

    // excludeFromTargets: the unit that just died, for AllOtherEnemies on-kill effects
    // forcedTarget: used by SameAsAutoAttack, bypasses targetFilter resolution entirely
    public void Execute(BattleUnit caster, BattleContext context, BattleUnit excludeFromTargets = null, BattleUnit forcedTarget = null)
    {
        if (customExecutor != null)
        {
            customExecutor.ExecuteCustom(caster, context.allyTeam, context.enemyTeam, context);
            return;
        }

        List<BattleUnit> targets;
        if (targetFilter == TargetFilterType.SameAsAutoAttack)
        {
            targets = new List<BattleUnit>();
            if (forcedTarget != null && forcedTarget.IsAlive) targets.Add(forcedTarget);
        }
        else
        {
            targets = TargetResolver.Resolve(targetFilter, caster, context, excludeFromTargets);
        }

        if (targets.Count == 0) return;

        foreach (BaseEffect effect in effects)
        {
            effect.Execute(caster, targets, context);
        }
    }
}