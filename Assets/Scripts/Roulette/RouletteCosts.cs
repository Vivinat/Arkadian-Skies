// Values taken directly from REGRAS.docx:
// Common: 5 to spin, 3 to acquire | Epic: 10 to spin, 5 to acquire | Legendary: 16 to spin, 8 to acquire
// Selling refunds half the acquire cost, rounded down (1 / 2 / 4).
public static class RouletteCosts
{
    public static int SpinCost(ChampionRarity rarity)
    {
        switch (rarity)
        {
            case ChampionRarity.Common: return 5;
            case ChampionRarity.Epic: return 10;
            case ChampionRarity.Legendary: return 16;
            default: return 0;
        }
    }

    public static int AcquireCost(ChampionRarity rarity)
    {
        switch (rarity)
        {
            case ChampionRarity.Common: return 3;
            case ChampionRarity.Epic: return 5;
            case ChampionRarity.Legendary: return 8;
            default: return 0;
        }
    }

    public static int SellValue(ChampionRarity rarity)
    {
        return AcquireCost(rarity) / 2;
    }
}