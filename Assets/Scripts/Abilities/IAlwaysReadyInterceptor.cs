// Marker interface (no methods): an interceptor that should apply regardless of the unit's Mana
// state, bypassing the generic Mana-readiness gate in CombatResolver.TryGetInterceptor. Needed for
// passives that must stay always-on even on a champion whose OTHER position uses Mana - e.g. Luna's
// Papillon block, which stays active on the Backline even though her Frontline Singularity is
// Mana-gated (both positions share the same CharacterData.manaPerSecond stat).
public interface IAlwaysReadyInterceptor
{
}
