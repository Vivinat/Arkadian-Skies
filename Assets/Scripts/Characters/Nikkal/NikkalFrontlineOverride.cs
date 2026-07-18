using UnityEngine;

[CreateAssetMenu(fileName = "NikkalFrontlineOverride", menuName = "Autobattler/Overrides/Nikkal - Elder's Repositioning")]
public class NikkalFrontlineOverride : AbilityOverrideSO
{
    // Swaps Nikkal with the ally directly behind her in the backline. If that slot is empty,
    // she simply takes it. Only NIKKAL trades kits: she explicitly switches to her backline
    // ability, while the moved ally keeps the kit of its original position (a Backline Luna
    // pushed into the frontline slot keeps using Papillon).
    public override void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context)
    {
        if (caster.position != BattlePosition.Frontline) return;

        BattleGrid grid = context.GetGridOf(caster);
        int slot = caster.slotIndex;
        BattleUnit allyBehind = grid.GetUnitAt(BattlePosition.Backline, slot);

        grid.RemoveUnit(caster);

        if (allyBehind != null)
        {
            grid.RemoveUnit(allyBehind);
            grid.PlaceUnit(allyBehind, BattlePosition.Frontline, slot);
        }

        grid.PlaceUnit(caster, BattlePosition.Backline, slot);
        caster.abilityPosition = BattlePosition.Backline;

        Debug.Log($"{BattleLog.LabelOf(caster)} uses Elder's Repositioning, permanently moving to the backline.");
        context.events.RaisePositionsChanged();
    }
}