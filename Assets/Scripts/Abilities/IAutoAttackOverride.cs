// Lets an ability completely replace the default tick-based auto-attack in BattleSimulator,
// for champions whose basic attack is gated by a custom resource or uses a non-standard formula
// (e.g. Luna's Papillon, which only fires while she has a Butterfly banked).
public interface IAutoAttackOverride
{
    bool CanAutoAttack(BattleUnit self);

    // Lets the override pick a different target than the standard frontline-first pick
    // (e.g. Luna always shooting an enemy Theodor who is on The Betrayer). Return defaultTarget
    // to keep the standard pick.
    BattleUnit ResolveAutoAttackTarget(BattleUnit self, BattleContext context, BattleUnit defaultTarget);

    void ExecuteAutoAttack(BattleUnit self, BattleUnit target, BattleContext context);
}
