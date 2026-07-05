using UnityEngine;

[CreateAssetMenu(fileName = "JacoboFrontlineOverride", menuName = "Autobattler/Overrides/Jacobo - Cignakalt")]
public class JacoboFrontlineOverride : AbilityOverrideSO, IDamageInterceptor
{
    [Range(0f, 1f)] public float ignorePercent = 0.75f;

    public override void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context) { }

    public float OnBeforeDamage(BattleUnit self, BattleUnit attacker, float incomingDamage, DamageType damageType, BattleContext context, out bool triggered)
    {
        triggered = false;
        if (attacker == null || !attacker.IsAlive) return incomingDamage;

        triggered = true;
        float ignoredAmount = incomingDamage * ignorePercent;
        float remainingDamage = incomingDamage - ignoredAmount;

        CombatResolver.ApplyDamage(self, attacker, ignoredAmount, DamageType.AD, context, allowInterception: false);
        Debug.Log($"{BattleLog.LabelOf(self)} uses Cignakalt - Devour & Burst, firing {ignoredAmount:F1} AD back at {BattleLog.LabelOf(attacker)}.");

        return remainingDamage;
    }
}