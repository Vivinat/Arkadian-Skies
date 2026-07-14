using UnityEngine;

[CreateAssetMenu(fileName = "EuphrosyneFrontlineOverride", menuName = "Autobattler/Overrides/Euphrosyne - Point-Blank Doomsday")]
public class EuphrosyneFrontlineOverride : AbilityOverrideSO
{
    public float damagePercentOfAP = 1.3f;
    [Range(0f, 1f)] public float apGainOnKillPercent = 0.10f;
    public float severeWoundsDuration = 10f;

    // Blasts the lowest-HP enemy. A kill grants Euphrosyne a permanent AP boost for the rest of the
    // battle (stacks per kill). A surviving target is afflicted with Severe Wounds, unable to be
    // healed for severeWoundsDuration seconds.
    public override void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context)
    {
        BattleGrid enemyGrid = context.GetEnemyGridOf(caster);
        BattleUnit target = TargetResolver.GetLowestHPAlive(enemyGrid.GetAllAlive());
        if (target == null) return;

        float rawDamage = caster.EffectiveAP * damagePercentOfAP;
        CombatResolver.ApplyDamage(caster, target, rawDamage, DamageType.AP, context);

        if (!target.IsAlive)
        {
            float apGain = caster.EffectiveAP * apGainOnKillPercent;
            caster.bonusAP += apGain;
            Debug.Log($"{BattleLog.LabelOf(caster)} uses Point-Blank Doomsday, killing {BattleLog.LabelOf(target)} and permanently gaining {apGain:F1} AP.");
        }
        else
        {
            CombatResolver.ApplyHealBlock(target, severeWoundsDuration, context);
            Debug.Log($"{BattleLog.LabelOf(caster)} uses Point-Blank Doomsday on {BattleLog.LabelOf(target)}.");
        }
    }
}