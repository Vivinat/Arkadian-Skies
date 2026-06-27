public interface IAbilityOverride
{
    void ExecuteCustom(BattleUnit caster, BattleGrid allySide, BattleGrid enemySide, BattleContext context);
}