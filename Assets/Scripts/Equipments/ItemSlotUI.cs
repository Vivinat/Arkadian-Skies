using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One visual bank slot. Only knows its own bank index and forwards drag events to the
// controller, the same way ChampionSlotUI forwards everything to PartyBenchUIController.
public class ItemSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image iconImage;
    public ItemTooltipTrigger tooltipTrigger;

    public int BankIndex { get; private set; }
    public ItemComponentData Item { get; private set; }
    public bool HasItem => Item != null;

    ItemBankUIController controller;

    public void Initialize(ItemBankUIController owner)
    {
        controller = owner;
    }

    // Keeps the Image enabled even when empty, same reasoning as ChampionSlotUI.SetChampion -
    // a disabled Graphic is skipped entirely by raycasts.
    public void SetItem(ItemComponentData newItem, int bankIndex)
    {
        Item = newItem;
        BankIndex = bankIndex;

        iconImage.sprite = newItem != null ? newItem.icon : null;
        iconImage.color = new Color(1f, 1f, 1f, newItem != null ? 1f : 0f);

        tooltipTrigger.SetItem(newItem);
    }

    public void SetDimmed(bool dimmed)
    {
        Color c = iconImage.color;
        c.a = dimmed ? 0.35f : (Item != null ? 1f : 0f);
        iconImage.color = c;
    }

    // Called by ChampionSlotUI.OnDrop when this slot's icon was the one dragged onto a portrait.
    public void NotifyDroppedOnCharacter(CharacterData champion)
    {
        controller.RequestEquip(this, champion);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem) return;
        controller.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData) => controller.UpdateDrag(eventData);

    public void OnEndDrag(PointerEventData eventData) => controller.EndDrag(eventData);
}