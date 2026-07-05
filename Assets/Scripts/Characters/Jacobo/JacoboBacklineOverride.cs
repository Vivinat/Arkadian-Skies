using UnityEngine;

[CreateAssetMenu(fileName = "JacoboBacklineOverride", menuName = "Autobattler/Overrides/Jacobo - With Me")]
public class JacoboBacklineOverride : AbilityOverrideSO, IAllyAbilityReactor
{
    public float damagePercentOfAD = 1.3f;
    [Range(0f, 1f)] public float defMdefReductionPercent = 0.40f;
    public float debuffDuration = 2f;
    [Range(0f, 1f)] public float hpThreshold = 0.5f;

    public override void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context) { }

    public bool OnAllySingleTargetAbility(BattleUnit self, BattleUnit allyCaster, BattleUnit target, BattleContext context)
    {
        if (!self.IsAlive || !target.IsAlive) return false;
        if (target.currentHP <= target.data.maxHP * hpThreshold) return false;

        float rawDamage = self.data.AD * damagePercentOfAD;
        CombatResolver.ApplyDamage(self, target, rawDamage, DamageType.AD, context);

        if (target.IsAlive)
            target.ApplyDefMdefDebuff(1f - defMdefReductionPercent, debuffDuration);

        Debug.Log($"{BattleLog.LabelOf(self)} uses With me!, striking {BattleLog.LabelOf(target)} before {BattleLog.LabelOf(allyCaster)}'s ability.");
        return true;
    }
}