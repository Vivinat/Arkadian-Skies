// Lets an ability show its own stack-based resource as pips in ResourceBarView, the same visual
// style already used for EveryNAutoAttacks champions (e.g. Aramor), but driven by an actual
// gain/spend currency instead of a modulo counter (e.g. Luna's Butterflies).
public interface IStackResourceDisplay
{
    int GetCurrentStacks(BattleUnit self);
    int GetMaxStacks(BattleUnit self);
}
