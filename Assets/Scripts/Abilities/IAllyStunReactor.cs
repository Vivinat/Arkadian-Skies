public interface IAllyStunReactor
{
    // returns true only if the reaction actually fired (used to decide whether the resource should be spent)
    bool OnAllyStunned(BattleUnit self, BattleUnit stunnedAlly, BattleContext context);
}