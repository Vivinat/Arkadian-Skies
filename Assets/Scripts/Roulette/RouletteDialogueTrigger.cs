using UnityEngine;

// Sits next to RouletteManager in the scene. Turns its acquisition/level-up events into
// dialogue: a champion speaks its Select Quote the first time it's bought, and its Level Up
// Quote every time it levels up from mementos.
public class RouletteDialogueTrigger : MonoBehaviour
{
    public RouletteManager roulette;
    public DialogueUIController dialogueUI;

    void OnEnable()
    {
        roulette.OnChampionAcquired += HandleAcquired;
        roulette.OnChampionLeveledUp += HandleLeveledUp;
    }

    void OnDisable()
    {
        roulette.OnChampionAcquired -= HandleAcquired;
        roulette.OnChampionLeveledUp -= HandleLeveledUp;
    }

    void HandleAcquired(CharacterData champion)
    {
        dialogueUI.Show(champion, ChampionQuoteType.Select);
    }

    void HandleLeveledUp(CharacterData champion, int newLevel)
    {
        dialogueUI.Show(champion, ChampionQuoteType.LevelUp);
    }
}