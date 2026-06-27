using UnityEngine;

public abstract class AbilityOverrideSO : ScriptableObject, IAbilityOverride
{
    public abstract void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context);
}