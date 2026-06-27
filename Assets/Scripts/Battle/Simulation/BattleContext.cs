public class BattleContext
{
    public BattleGrid allySide;
    public BattleGrid enemySide;
    public BattleEvents events;

    public BattleContext(BattleGrid allySide, BattleGrid enemySide, BattleEvents events)
    {
        this.allySide = allySide;
        this.enemySide = enemySide;
        this.events = events;
    }

    public BattleGrid GetGridOf(BattleUnit unit)
    {
        return unit.side == BattleSide.Ally ? allySide : enemySide;
    }

    public BattleGrid GetEnemyGridOf(BattleUnit unit)
    {
        return unit.side == BattleSide.Ally ? enemySide : allySide;
    }
}