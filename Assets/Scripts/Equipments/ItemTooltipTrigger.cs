using UnityEngine;
using UnityEngine.EventSystems;

// Hooks up any item icon to the shared AbilityTooltip panel - same reusable tooltip the ability
// icons already use (it only needs a title + description string), just fed an item's own
// description plus its formatted stat lines instead.
public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    ItemComponentData item;

    public void SetItem(ItemComponentData newItem)
    {
        item = newItem;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null) return;
        AbilityTooltip.Instance?.Show(item.itemName, BuildDescription(item));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AbilityTooltip.Instance?.Hide();
    }

    static string BuildDescription(ItemComponentData item)
    {
        string text = item.description;

        foreach (StatModifier modifier in item.modifiers)
        {
            string line = FormatModifier(modifier);
            text = string.IsNullOrEmpty(text) ? line : $"{text}\n{line}";
        }

        return text;
    }

    static string FormatModifier(StatModifier modifier)
    {
        string label = LabelFor(modifier.statType);

        // CritChance is stored as a 0-1 fraction even when the modifier is Flat (it mirrors
        // CharacterData.critChance), so it still needs the *100 conversion to read as a percentage.
        bool isCritChanceFlat = modifier.statType == StatType.CritChance && modifier.modifierType == ModifierType.Flat;
        float displayValue = isCritChanceFlat ? modifier.value * 100f : modifier.value;
        bool showsAsPercent = modifier.modifierType == ModifierType.PercentOfBase || isCritChanceFlat;

        string sign = displayValue >= 0f ? "+" : "";
        return showsAsPercent ? $"{sign}{displayValue:0.##}% {label}" : $"{sign}{displayValue:0.##} {label}";
    }

    static string LabelFor(StatType type)
    {
        switch (type)
        {
            case StatType.MaxHP: return "HP";
            case StatType.AD: return "AD";
            case StatType.AP: return "AP";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.CritChance: return "Crit Chance";
            case StatType.CritDamage: return "Crit Damage";
            case StatType.DEF: return "DEF";
            case StatType.MDEF: return "MDEF";
            case StatType.ManaPerSecond: return "Mana/s";
            default: return type.ToString();
        }
    }
}