using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDamageEffect", menuName = "Autobattler/Effects/Damage")]
public class DamageEffect : BaseEffect
{
    public DamageType damageType = DamageType.AD;

    [Header("Raw damage formula (added together)")]
    public float percentOfCasterAD;
    public float percentOfCasterAP;
    public float flatDamage;

    [Tooltip("If set above 0, ignores caster stats and instead deals this percent of the TARGET's max HP as damage (e.g. Aramor's true damage effects).")]
    public float percentOfTargetMaxHP;

    public override void Execute(BattleUnit caster, List<BattleUnit> targets, BattleContext context)
    {
        foreach (BattleUnit target in targets)
        {
            if (!target.IsAlive) continue;

            float rawDamage;
            if (percentOfTargetMaxHP > 0f)
                rawDamage = target.data.maxHP * percentOfTargetMaxHP;
            else
                rawDamage = (caster.data.AD * percentOfCasterAD) + (caster.data.AP * percentOfCasterAP) + flatDamage;

            float defense = damageType == DamageType.AP ? target.data.MDEF : target.data.DEF;
            float damage = DamageCalculator.CalculateFinalDamage(rawDamage, defense, damageType);

            target.TakeDamage(damage);

            Debug.Log($"{BattleLog.LabelOf(caster)} deals {damage:F1} {damageType} damage to {BattleLog.LabelOf(target)}. {BattleLog.LabelOf(target)} HP: {target.currentHP:F1}/{target.data.maxHP}");

            if (!target.IsAlive)
            {
                Debug.Log($"{BattleLog.LabelOf(target)} has died.");
                context.events.RaiseKill(caster, target);
            }
        }
    }
}