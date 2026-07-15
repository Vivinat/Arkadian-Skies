using UnityEngine;

[CreateAssetMenu(fileName = "LunaFrontlineOverride", menuName = "Autobattler/Overrides/Luna - Singularity")]
public class LunaFrontlineOverride : AbilityOverrideSO, IDamageInterceptor
{
    [Header("Black Hole (defensive stage)")]
    [Range(0f, 1f)] public float blockPercent = 0.5f;
    [Range(0f, 1f)] public float absorbPercent = 0.5f;
    public int attacksPerStage = 3;

    [Header("White Hole (offensive stage)")]
    [Range(0f, 1f)] public float halveOnReleasePercent = 0.5f;
    public DamageType releaseDamageType = DamageType.AD;

    // Per-battle state, stashed on BattleUnit.championState instead of on this ScriptableObject
    // (which is shared across every battle/unit that uses this asset).
    class SingularityState
    {
        public bool inWhiteHole;
        public int attacksThisStage;
        public float absorbedDamage;
        public float releasePerHit;
    }

    // Singularity never casts through Ability.Execute, so this stays empty - all of it happens
    // through OnBeforeDamage below.
    public override void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context) { }

    // Mana-gated, same pattern as Jacobo's Cignakalt: CombatResolver.TryGetInterceptor only calls
    // this once Luna's Mana (Luna.asset's manaPerSecond) is full, and resets it to 0 afterwards -
    // nothing in this file checks Mana directly. Each time it fires: in Black Hole, blocks
    // blockPercent of the hit and banks absorbPercent of its raw damage; on the 3rd such hit she
    // flips to White Hole, where the bank is halved and split evenly across the next
    // attacksPerStage hits, which land at full damage but pay back releasePerHit to the attacker.
    // After that she flips back to Black Hole and repeats.
    public float OnBeforeDamage(BattleUnit self, BattleUnit attacker, float incomingDamage, DamageType damageType, BattleContext context, out bool triggered)
    {
        triggered = true;
        SingularityState state = GetState(self);
        state.attacksThisStage++;

        float resultingDamage = incomingDamage;

        if (!state.inWhiteHole)
        {
            resultingDamage = incomingDamage * (1f - blockPercent);
            state.absorbedDamage += incomingDamage * absorbPercent;

            if (state.attacksThisStage >= attacksPerStage)
                EnterWhiteHole(self, state);
        }
        else
        {
            if (attacker != null && attacker.IsAlive && state.releasePerHit > 0f)
                CombatResolver.ApplyDamage(self, attacker, state.releasePerHit, releaseDamageType, context, allowInterception: false);

            if (state.attacksThisStage >= attacksPerStage)
                EnterBlackHole(state);
        }

        return resultingDamage;
    }

    void EnterWhiteHole(BattleUnit self, SingularityState state)
    {
        state.inWhiteHole = true;
        state.attacksThisStage = 0;
        state.absorbedDamage *= (1f - halveOnReleasePercent);
        state.releasePerHit = state.absorbedDamage / attacksPerStage;
        Debug.Log($"{BattleLog.LabelOf(self)}'s Singularity flips to White Hole, releasing {state.releasePerHit:F1} {releaseDamageType} per hit.");
    }

    void EnterBlackHole(SingularityState state)
    {
        state.inWhiteHole = false;
        state.attacksThisStage = 0;
        state.absorbedDamage = 0f;
        state.releasePerHit = 0f;
    }

    static SingularityState GetState(BattleUnit self)
    {
        if (!(self.championState is SingularityState state))
        {
            state = new SingularityState();
            self.championState = state;
        }
        return state;
    }
}
