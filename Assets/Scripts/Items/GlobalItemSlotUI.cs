using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One global item stack slot: icon + stack count, name text only as a fallback when the
// item has no icon. Hover tooltips the description; clicks go to the bank UI.
public class GlobalItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button button;
    public Image iconImage;
    public TMP_Text countLabel;
    public TMP_Text fallbackNameLabel;

    public int Index { get; private set; }
    public GlobalItemData Item { get; private set; }

    GlobalItemBankUI controller;

    public void Initialize(GlobalItemBankUI owner, int index)
    {
        controller = owner;
        Index = index;
        button.onClick.AddListener(() => controller.OnSlotClicked(Index));
    }

    public void SetStack(GlobalItemData item, int count)
    {
        Item = item;

        bool hasIcon = item != null && item.icon != null;
        iconImage.sprite = hasIcon ? item.icon : null;
        iconImage.enabled = hasIcon;
        fallbackNameLabel.text = item != null && !hasIcon ? item.itemName : "";
        countLabel.text = count > 1 ? $"x{count}" : "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Item == null) return;
        AbilityTooltip.Instance?.Show(Item.itemName, Item.description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AbilityTooltip.Instance?.Hide();
    }
}
