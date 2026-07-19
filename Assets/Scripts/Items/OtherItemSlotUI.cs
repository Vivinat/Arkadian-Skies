using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One map-consumable stack slot. Drag-based items (Disassembler, Delirium, Nostalgia)
// forward drag events to the controller; Artisan's Aspect uses a left click instead.
public class OtherItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Image iconImage;
    public TMP_Text countLabel;
    public TMP_Text fallbackNameLabel;

    public int Index { get; private set; }
    public OtherItemData Item { get; private set; }
    public bool HasItem => Item != null;

    OtherItemBankUI controller;

    public void Initialize(OtherItemBankUI owner, int index)
    {
        controller = owner;
        Index = index;
    }

    public void SetStack(OtherItemData item, int count)
    {
        Item = item;

        bool hasIcon = item != null && item.icon != null;
        iconImage.sprite = hasIcon ? item.icon : null;
        iconImage.enabled = hasIcon;
        fallbackNameLabel.text = item != null && !hasIcon ? item.itemName : "";
        countLabel.text = count > 1 ? $"x{count}" : "";
    }

    bool IsDragItem => Item != null && Item.effectType != OtherItemEffectType.ArtisanAspect;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsDragItem) return;
        controller.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsDragItem) controller.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsDragItem) controller.EndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !HasItem) return;
        if (Item.effectType == OtherItemEffectType.ArtisanAspect) controller.UseArtisan(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Item != null) AbilityTooltip.Instance?.Show(Item.itemName, Item.description);
    }

    public void OnPointerExit(PointerEventData eventData) => AbilityTooltip.Instance?.Hide();
}
