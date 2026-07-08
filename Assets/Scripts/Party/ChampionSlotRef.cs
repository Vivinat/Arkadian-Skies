public enum SlotContainer
{
    Bench,
    Party
}

// Identifies one slot, whether it's a bench slot or a party slot, so PartyManager can offer a
// single generic move/swap operation instead of separate bench/party-specific methods.
public struct ChampionSlotRef
{
    public SlotContainer container;
    public BattlePosition position; // only meaningful when container == Party
    public int index;

    public static ChampionSlotRef BenchSlot(int index)
    {
        return new ChampionSlotRef { container = SlotContainer.Bench, index = index };
    }

    public static ChampionSlotRef PartySlot(BattlePosition position, int index)
    {
        return new ChampionSlotRef { container = SlotContainer.Party, position = position, index = index };
    }

    public bool Equals(ChampionSlotRef other)
    {
        return container == other.container && position == other.position && index == other.index;
    }

    public override bool Equals(object obj) => obj is ChampionSlotRef other && Equals(other);

    public override int GetHashCode()
    {
        return ((int)container * 397 ^ (int)position) * 397 ^ index;
    }

    public static bool operator ==(ChampionSlotRef a, ChampionSlotRef b) => a.Equals(b);
    public static bool operator !=(ChampionSlotRef a, ChampionSlotRef b) => !a.Equals(b);
}