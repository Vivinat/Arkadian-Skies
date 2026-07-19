using System.Collections.Generic;
using UnityEngine;

// Equipment data, used both for raw components (Dagger, Long Sword...) and for complete
// items forged from two components. Complete items carry their recipe plus a passive
// description; passive EFFECTS are integrated into battle separately, per item.
[CreateAssetMenu(fileName = "NewItemComponent", menuName = "Autobattler/Items/Item Component")]
public class ItemComponentData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    public List<StatModifier> modifiers = new List<StatModifier>();

    [Header("Complete item (forged from two components)")]
    public bool isCompleteItem;
    public ItemComponentData recipeComponentA;
    public ItemComponentData recipeComponentB;

    // Unordered recipe match
    public bool MatchesRecipe(ItemComponentData a, ItemComponentData b)
    {
        if (!isCompleteItem || recipeComponentA == null || recipeComponentB == null) return false;
        return (recipeComponentA == a && recipeComponentB == b)
            || (recipeComponentA == b && recipeComponentB == a);
    }
}