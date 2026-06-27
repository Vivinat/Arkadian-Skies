using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityOverrideSO : ScriptableObject, IAbilityOverride
{
    public abstract void ExecuteCustom(BattleUnit caster, List<BattleUnit> allyTeam, List<BattleUnit> enemyTeam, BattleContext context);
}