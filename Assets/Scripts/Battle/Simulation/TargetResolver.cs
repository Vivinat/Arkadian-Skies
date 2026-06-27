using System.Collections.Generic;

public static class TargetResolver
{
    // excludeUnit is only used by AllOtherEnemies (the unit that just died, to exclude from the on-kill nova)
    public static List<BattleUnit> Resolve(TargetFilterType filter, BattleUnit caster, BattleContext context, BattleUnit excludeUnit = null)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        switch (filter)
        {
            case TargetFilterType.Self:
                result.Add(caster);
                break;

            case TargetFilterType.LowestHPEnemy:
                {
                    BattleUnit lowest = GetLowestHPAlive(context.GetEnemiesOf(caster));
                    if (lowest != null) result.Add(lowest);
                    break;
                }

            case TargetFilterType.HighestHPEnemy:
                {
                    BattleUnit highest = GetHighestHPAlive(context.GetEnemiesOf(caster));
                    if (highest != null) result.Add(highest);
                    break;
                }

            case TargetFilterType.AllEnemies:
                foreach (BattleUnit unit in context.GetEnemiesOf(caster))
                    if (unit.IsAlive) result.Add(unit);
                break;

            case TargetFilterType.AllOtherEnemies:
                foreach (BattleUnit unit in context.GetEnemiesOf(caster))
                    if (unit.IsAlive && unit != excludeUnit) result.Add(unit);
                break;
        }

        return result;
    }

    static BattleUnit GetLowestHPAlive(List<BattleUnit> team)
    {
        BattleUnit lowest = null;
        foreach (BattleUnit unit in team)
        {
            if (!unit.IsAlive) continue;
            if (lowest == null || unit.currentHP < lowest.currentHP)
                lowest = unit;
        }
        return lowest;
    }

    static BattleUnit GetHighestHPAlive(List<BattleUnit> team)
    {
        BattleUnit highest = null;
        foreach (BattleUnit unit in team)
        {
            if (!unit.IsAlive) continue;
            if (highest == null || unit.currentHP > highest.currentHP)
                highest = unit;
        }
        return highest;
    }
}