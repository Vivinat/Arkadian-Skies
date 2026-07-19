using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One "A + B = Result" row in the recipe panel. Each icon carries an ItemTooltipTrigger
// so hovering shows that item's full description and stats.
public class ItemRecipeRowUI : MonoBehaviour
{
    public Image iconA;
    public Image iconB;
    public Image iconResult;
    public ItemTooltipTrigger tooltipA;
    public ItemTooltipTrigger tooltipB;
    public ItemTooltipTrigger tooltipResult;
    public TMP_Text resultNameLabel;

    public void Bind(ItemComponentData a, ItemComponentData b, ItemComponentData result)
    {
        gameObject.SetActive(true);
        SetIcon(iconA, tooltipA, a);
        SetIcon(iconB, tooltipB, b);
        SetIcon(iconResult, tooltipResult, result);
        resultNameLabel.text = result != null ? result.itemName : "";
    }

    public void Clear()
    {
        gameObject.SetActive(false);
    }

    static void SetIcon(Image image, ItemTooltipTrigger tooltip, ItemComponentData item)
    {
        image.sprite = item != null ? item.icon : null;
        image.enabled = item != null && item.icon != null;
        tooltip.SetItem(item);
    }
}
