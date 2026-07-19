using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One of the 3 equipment icons shown in the Character Info Panel. Left-click on an occupied
// slot unequips that item back to the bank - the drag side (equipping) all happens on the
// ItemSlotUI/ItemBankUIController side instead.
public class EquippedItemSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public Image iconImage;
    public ItemTooltipTrigger tooltipTrigger;

    // Optional: when set (battle scene), equipping and unequipping only work while paused
    public BattlePauseController pauseGate;

    CharacterEquipmentManager manager;
    CharacterData champion;
    int slotIndex;

    bool EquipLocked => pauseGate != null && !pauseGate.IsPaused;

    public void Bind(CharacterEquipmentManager equipmentManager, CharacterData forChampion, int index)
    {
        manager = equipmentManager;
        champion = forChampion;
        slotIndex = index;

        ItemComponentData item = manager.GetEquippedAt(champion, slotIndex);
        iconImage.sprite = item != null ? item.icon : null;
        iconImage.color = new Color(1f, 1f, 1f, item != null ? 1f : 0f);
        tooltipTrigger.SetItem(item);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (EquipLocked) return;
        if (manager == null || manager.GetEquippedAt(champion, slotIndex) == null) return;

        if (pauseGate != null && pauseGate.AvailablePauses <= 0)
        {
            pauseGate.NotifyChargeDenied("No pause charges left to unequip! 1 action = 1 charge.");
            return;
        }

        if (manager.Unequip(champion, slotIndex) && pauseGate != null) pauseGate.TrySpendPause();
    }

    // Dropping an item icon from the bank onto this slot equips it - only for champions
    // the player actually owns (party or bench), never roulette previews
    public void OnDrop(PointerEventData eventData)
    {
        if (EquipLocked) return;
        ItemSlotUI droppedItem = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<ItemSlotUI>() : null;
        if (droppedItem == null || !droppedItem.HasItem || manager == null || champion == null) return;
        if (manager.roster != null && !manager.roster.Owns(champion)) return;

        if (pauseGate != null && pauseGate.AvailablePauses <= 0)
        {
            pauseGate.NotifyChargeDenied("No pause charges left to equip! 1 action = 1 charge.");
            return;
        }

        bool equipped = manager.GetEquippedAt(champion, slotIndex) == null
            ? manager.Equip(champion, droppedItem.BankIndex, slotIndex)
            : manager.EquipToFirstEmptySlot(champion, droppedItem.BankIndex);

        if (equipped && pauseGate != null) pauseGate.TrySpendPause();
    }
}