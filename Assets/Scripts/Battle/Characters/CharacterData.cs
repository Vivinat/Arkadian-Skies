using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Autobattler/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite portrait;

    [Header("Roulette")]
    public ChampionRarity rarity;
    
    [Header("Leveling")]
    public CharacterClass characterClass;

    [Header("Base Stats")]
    public float maxHP;
    public float AD;
    public float AP;
    public float attackSpeed; // attacks per second
    public float critChance; // 0 to 1
    public float DEF;
    public float MDEF;
    public float manaPerSecond; // passive mana regen, 0 if not mana-based

    [Header("Abilities")]
    public Ability frontlineAbility;
    public Ability backlineAbility;
}