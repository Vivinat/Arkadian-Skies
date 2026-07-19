using UnityEngine;

public enum OtherItemEffectType
{
    Disassembler,   // hold over a complete item -> back to its two components
    Delirium,       // hold over a complete item -> a different random complete item
    ArtisanAspect,  // left click -> choose 1 of 5 random complete items
    Nostalgia       // hold over a champion -> a random champion of the same cost
}

// A map-only consumable ("Outros Items"): used on the level map, never in battle.
[CreateAssetMenu(fileName = "OtherItem", menuName = "Autobattler/Items/Other Item")]
public class OtherItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public OtherItemEffectType effectType;
}
