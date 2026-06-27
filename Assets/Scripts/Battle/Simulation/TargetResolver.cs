using System.Collections.Generic;

public static class TargetResolver
{
    // excludeUnit is only used by AllOtherEnemies (the unit that just died, to exclude from the on-kill nova)
    public static List<BattleUnit> Resolve(TargetFilterType filter, BattleUnit caster, BattleContext context, BattleUnit excludeUnit = null)
    {
        List<BattleUnit> result = new List<BattleUnit>();
        List<BattleUnit> aliveEnemies = context.GetEnemyGridOf(caster).GetAllAlive();

        switch (filter)
        {
            case TargetFilterType.Self:
                result.Add(caster);
                break;

            case TargetFilterType.LowestHPEnemy:
                {
                    BattleUnit lowest = GetLowestHPAlive(aliveEnemies);
                    if (lowest != null) result.Add(lowest);
                    break;
                }

            case TargetFilterType.HighestHPEnemy:
                {
                    BattleUnit highest = GetHighestHPAlive(aliveEnemies);
                    if (highest != null) result.Add(highest);
                    break;
                }

            case TargetFilterType.AllEnemies:
                foreach (BattleUnit unit in aliveEnemies)
                    result.Add(unit);
                break;

            case TargetFilterType.AllOtherEnemies:
                foreach (BattleUnit unit in aliveEnemies)
                    if (unit != excludeUnit) result.Add(unit);
                break;
        }

        return result;
    }

    static BattleUnit GetLowestHPAlive(List<BattleUnit> team)
    {
        BattleUnit lowest = null;
        foreach (BattleUnit unit in team)
        {
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
            if (highest == null || unit.currentHP > highest.currentHP)
                highest = unit;
        }
        return highest;
    }
}