using UnityEngine;

public enum GlobalItemEffectType
{
    DamageAllEnemies,       // Ruthanian Anarchy Protocol
    HealAllAllies,          // Halcyon Dream
    HealAndRegenAllies,     // Blood of the First Pures: Unleashed
    WoundsAndBleedEnemies,  // Erradicator's Final Verdict
    ReviveFallenAllies,     // The Necromancer's Call
    TeamLifesteal,          // Conviction
    ReflectDamage,          // Delphinium's Mirror Shard
    DoubleTeamHP,           // Schemes of the Viper (not implemented yet)
    BlindAndSilence         // Vial of the Great Sea of Ebony (not implemented yet)
}

// A global item: usable only during a battle pause, one per pause, consumed on use.
[CreateAssetMenu(fileName = "GlobalItem", menuName = "Autobattler/Items/Global Item")]
public class GlobalItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public GlobalItemEffectType effectType;
    public float power;    // meaning depends on the effect (flat damage, heal, percent as 0-1...)
    public float duration; // seconds, for timed effects
}
