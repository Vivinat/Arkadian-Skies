using System.Collections.Generic;

public interface IAbilityOverride
{
    void ExecuteCustom(BattleUnit caster, List<BattleUnit> allyTeam, List<BattleUnit> enemyTeam, BattleContext context);
}