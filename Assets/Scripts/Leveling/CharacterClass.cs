// Matches the six columns of the "TABELA DE LEVEL-UP" in REGRAS.docx.
// Set this per CharacterData asset in the Inspector - it decides which stat gains a champion
// receives on level-up, independent of ChampionRarity (which only affects Roulette costs/odds).
public enum CharacterClass
{
    Tank,
    OffTank,
    DPS,
    AP,
    Support,
    YuriaSpecial
}