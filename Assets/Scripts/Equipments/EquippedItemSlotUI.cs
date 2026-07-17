using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One of the 3 equipment icons shown in the Character Info Panel. Left-click on an occupied
// slot unequips that item back to the bank - the drag side (equipping) all happens on the
// ItemSlotUI/ItemBankUIController side instead.
public class EquippedItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public ItemTooltipTrigger tooltipTrigger;

    CharacterEquipmentManager manager;
    CharacterData champion;
    int slotIndex;

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
        manager?.Unequip(champion, slotIndex);
    }
}