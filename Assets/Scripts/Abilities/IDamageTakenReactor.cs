// Reacts to damage a unit just took, AFTER interception/blocking already reduced it. Unlike
// IDamageInterceptor this cannot change the damage - it only observes it (e.g. Luna banking a
// Butterfly whenever Papillon is hit).
public interface IDamageTakenReactor
{
    void OnDamageTaken(BattleUnit self, BattleUnit attacker, float damageTaken, DamageType damageType, BattleContext context);
}
