using UnityEngine;

[CreateAssetMenu(fileName = "LunaBacklineOverride", menuName = "Autobattler/Overrides/Luna - Papillon")]
public class LunaBacklineOverride : AbilityOverrideSO, IDamageInterceptor, IAlwaysReadyInterceptor, IPassiveTicker, IDamageTakenReactor, IAutoAttackOverride, IStackResourceDisplay
{
    [Header("Passive Block")]
    [Range(0f, 1f)] public float blockPercent = 0.25f;

    [Header("Butterflies")]
    public float secondsPerButterfly = 1f;
    public float hitBonusCooldown = 5f;
    public int maxButterflies = 5;

    [Header("Shot")]
    [Range(0f, 2f)] public float shotDamagePercentOfAD = 1.0f;
    [Range(0f, 1f)] public float defIgnorePercent = 0.5f;

    [Header("Theodor Synergy")]
    public string betrayerCharacterName = "Theodor";

    // Per-battle state, stashed on BattleUnit.championState instead of on this ScriptableObject
    // (which is shared across every battle/unit that uses this asset).
    class PapillonState
    {
        public float butterflies;
        public float regenTimer;
        public float hitBonusCooldownRemaining;
    }

    // Papillon never casts through Ability.Execute, so this stays empty - all of it happens through
    // the interfaces below.
    public override void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context) { }

    // IAlwaysReadyInterceptor makes this block always active regardless of Luna's Mana - she's a
    // pure auto-attacker on the Backline, unrelated to the Mana gate her Frontline Singularity uses.
    public float OnBeforeDamage(BattleUnit self, BattleUnit attacker, float incomingDamage, DamageType damageType, BattleContext context, out bool triggered)
    {
        triggered = true;
        return incomingDamage * (1f - blockPercent);
    }

    public void TickPassive(BattleUnit self, float deltaTime, BattleContext context)
    {
        PapillonState state = GetState(self);

        state.regenTimer += deltaTime;
        while (state.regenTimer >= secondsPerButterfly)
        {
            state.regenTimer -= secondsPerButterfly;
            state.butterflies = Mathf.Min(state.butterflies + 1f, maxButterflies);
        }

        if (state.hitBonusCooldownRemaining > 0f)
            state.hitBonusCooldownRemaining -= deltaTime;
    }

    // Grants a bonus Butterfly whenever she's hit, on a 5s internal cooldown (separate from the
    // passive 1/s regen above).
    public void OnDamageTaken(BattleUnit self, BattleUnit attacker, float damageTaken, DamageType damageType, BattleContext context)
    {
        if (damageTaken <= 0f) return;

        PapillonState state = GetState(self);
        if (state.hitBonusCooldownRemaining > 0f) return;

        state.butterflies = Mathf.Min(state.butterflies + 1f, maxButterflies);
        state.hitBonusCooldownRemaining = hitBonusCooldown;
        Debug.Log($"{BattleLog.LabelOf(self)} gains a Butterfly from taking damage ({state.butterflies:F0} banked).");
    }

    public bool CanAutoAttack(BattleUnit self)
    {
        return GetState(self).butterflies >= 1f;
    }

    // If an enemy Theodor is on his Backline (The Betrayer), Luna always shoots him instead of the
    // standard frontline-first target.
    public BattleUnit ResolveAutoAttackTarget(BattleUnit self, BattleContext context, BattleUnit defaultTarget)
    {
        BattleGrid enemyGrid = context.GetEnemyGridOf(self);
        foreach (BattleUnit enemy in enemyGrid.GetAllAlive())
        {
            if (enemy.position == BattlePosition.Backline && enemy.data.characterName == betrayerCharacterName)
                return enemy;
        }
        return defaultTarget;
    }

    // Spends a Butterfly to shoot for shotDamagePercentOfAD, ignoring defIgnorePercent of the
    // target's DEF. A kill or a critical hit refunds the Butterfly (net-zero cost that shot).
    public void ExecuteAutoAttack(BattleUnit self, BattleUnit target, BattleContext context)
    {
        PapillonState state = GetState(self);
        state.butterflies -= 1f;

        float rawDamage = self.EffectiveAD * shotDamagePercentOfAD;

        bool didCrit = CritCalculator.Roll(self.EffectiveCritChance);
        if (didCrit) rawDamage *= CritCalculator.GetMultiplier(self.EffectiveCritDamage);

        bool wasAlreadyDead = !target.IsAlive;
        CombatResolver.ApplyDamage(self, target, rawDamage, DamageType.AD, context, defensePenetrationPercent: defIgnorePercent);
        bool killedTarget = !wasAlreadyDead && !target.IsAlive;

        if (killedTarget || didCrit)
        {
            state.butterflies = Mathf.Min(state.butterflies + 1f, maxButterflies);
            Debug.Log($"{BattleLog.LabelOf(self)}'s Papillon refunds a Butterfly ({(killedTarget ? "kill" : "crit")}).");
        }

        Debug.Log($"{BattleLog.LabelOf(self)} uses Papillon on {BattleLog.LabelOf(target)} - {state.butterflies:F0} Butterflies remaining.");
    }

    // Feeds ResourceBarView's pip display - same visual style as Aramor's EveryNAutoAttacks bar,
    // but showing actual banked Butterflies instead of a modulo counter.
    public int GetCurrentStacks(BattleUnit self) => Mathf.RoundToInt(GetState(self).butterflies);
    public int GetMaxStacks(BattleUnit self) => maxButterflies;

    static PapillonState GetState(BattleUnit self)
    {
        if (!(self.championState is PapillonState state))
        {
            state = new PapillonState();
            self.championState = state;
        }
        return state;
    }
}
