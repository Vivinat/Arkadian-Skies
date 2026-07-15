// Per-tick upkeep for passives that aren't Mana (e.g. Luna's Butterflies regenerating over time).
// Ticked once per unit per simulation tick in BattleSimulator, skipped while stunned - same as Mana.
public interface IPassiveTicker
{
    void TickPassive(BattleUnit self, float deltaTime, BattleContext context);
}
