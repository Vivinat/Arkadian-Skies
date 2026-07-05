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

    [Tooltip("If set above 0, ignores caster stats and instead deals this percent of the TARGET's max HP as damage.")]
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

            CombatResolver.ApplyDamage(caster, target, rawDamage, damageType, context);
        }
    }
}