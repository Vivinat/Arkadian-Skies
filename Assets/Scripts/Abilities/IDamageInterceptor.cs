public interface IDamageInterceptor
{
    float OnBeforeDamage(BattleUnit self, BattleUnit attacker, float incomingDamage, DamageType damageType, BattleContext context, out bool triggered);
}