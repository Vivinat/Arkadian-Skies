using UnityEngine;
using System.Collections.Generic;
public class BattleContext
{
    public List<BattleUnit> allyTeam;
    public List<BattleUnit> enemyTeam;
    public BattleEvents events;

    public BattleContext(List<BattleUnit> allyTeam, List<BattleUnit> enemyTeam, BattleEvents events)
    {
        this.allyTeam = allyTeam;
        this.enemyTeam = enemyTeam;
        this.events = events;
    }

    public List<BattleUnit> GetEnemiesOf(BattleUnit unit)
    {
        return unit.side == BattleSide.Ally ? enemyTeam : allyTeam;
    }

    public List<BattleUnit> GetAlliesOf(BattleUnit unit)
    {
        return unit.side == BattleSide.Ally ? allyTeam : enemyTeam;
    }
}