using UnityEngine;

[CreateAssetMenu(fileName = "NikkalBacklineOverride", menuName = "Autobattler/Overrides/Nikkal - Transmogryphy the Pain")]
public class NikkalBacklineOverride : AbilityOverrideSO, IAllyStunReactor
{
    [Range(0f, 1f)] public float healPercentOfAP = 0.10f;
    public float minManaToCleanse = 40f;

    // Mana-triggered active: heals the ally (on her own side) with the lowest current HP.
    public override void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context)
    {
        BattleGrid ownGrid = context.GetGridOf(caster);
        BattleUnit lowest = TargetResolver.GetLowestHPAlive(ownGrid.GetAllAlive());
        if (lowest == null) return;

        float healAmount = caster.EffectiveAP * healPercentOfAP;
        lowest.Heal(healAmount);

        Debug.Log($"{BattleLog.LabelOf(caster)} uses Transmogryphy the Pain, healing {BattleLog.LabelOf(lowest)} for {healAmount:F1}.");
    }

    // Reactive passive: if she has at least minManaToCleanse mana banked, immediately spends all of it
    // to remove a stun from an ally.
    public bool OnAllyStunned(BattleUnit self, BattleUnit stunnedAlly, BattleContext context)
    {
        if (!self.IsAlive || self.currentMana < minManaToCleanse) return false;

        stunnedAlly.ClearStun();
        Debug.Log($"{BattleLog.LabelOf(self)} uses Transmogryphy the Pain, cleansing the stun on {BattleLog.LabelOf(stunnedAlly)}.");
        return true;
    }
}