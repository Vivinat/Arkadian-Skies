// One of the up-to-5 "Lembranças" shown after a spin.
public class RouletteResult
{
    public CharacterData champion;
    public ChampionRarity rarity;
    public bool claimed;

    public RouletteResult(CharacterData champion, ChampionRarity rarity)
    {
        this.champion = champion;
        this.rarity = rarity;
        claimed = false;
    }
}