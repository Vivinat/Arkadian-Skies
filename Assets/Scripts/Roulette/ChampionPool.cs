using System.Collections.Generic;
using UnityEngine;

// Drag every CharacterData that should be rollable into availableChampions.
// Rarity is read from each CharacterData, so this list is the only thing you touch when adding new champions later.
[CreateAssetMenu(fileName = "NewChampionPool", menuName = "Autobattler/Roulette/Champion Pool")]
public class ChampionPool : ScriptableObject
{
    public List<CharacterData> availableChampions = new List<CharacterData>();

    public List<CharacterData> GetByRarity(ChampionRarity rarity)
    {
        List<CharacterData> result = new List<CharacterData>();
        foreach (CharacterData champion in availableChampions)
        {
            if (champion != null && champion.rarity == rarity) result.Add(champion);
        }
        return result;
    }
}