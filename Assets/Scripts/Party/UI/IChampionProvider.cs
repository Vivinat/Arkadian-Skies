// Implemented by whatever component already tracks "which CharacterData does this portrait
// represent" (ChampionSlotUI, RouletteSlotButton...). Lets CharacterInspectTrigger work on any
// of them without needing to know which one it's sitting next to.
public interface IChampionProvider
{
    CharacterData GetInspectedChampion();
}