public interface IAllyAbilityReactor
{
    // returns true only if the reaction actually fired (used to decide whether the resource should be spent)
    bool OnAllySingleTargetAbility(BattleUnit self, BattleUnit allyCaster, BattleUnit target, BattleContext context);
}