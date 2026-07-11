using System.Collections.Generic;
using UnityEngine;

// A component is a raw ingredient (Dagger, Long Sword, Staff...) - pure stat bonuses, no passives.
// Two of these combine into a complete item later (recipes/passives are a separate system).
[CreateAssetMenu(fileName = "NewItemComponent", menuName = "Autobattler/Items/Item Component")]
public class ItemComponentData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public List<StatModifier> modifiers = new List<StatModifier>();
}