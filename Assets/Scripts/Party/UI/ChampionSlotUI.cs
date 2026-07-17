using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SlotHighlight
{
    None,
    DropZone,
    Hovered
}

// One visual slot - either a bench slot or a party slot. Only knows its own ChampionSlotRef and
// forwards every drag/hover event to the controller, which owns all the actual game-state logic.
public class ChampionSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IChampionProvider
{
    public Image portraitImage;
    public Image highlightImage; // optional: a soft glow/outline sprite, shown while a drag is in progress

    public ChampionSlotRef SlotRef { get; private set; }
    public CharacterData Champion { get; private set; }
    public bool HasChampion => Champion != null;

    PartyBenchUIController controller;

    public void Initialize(PartyBenchUIController owner, ChampionSlotRef slotRef)
    {
        controller = owner;
        SlotRef = slotRef;

        if (highlightImage != null)
        {
            highlightImage.raycastTarget = false; // must never steal the drag/drop raycasts meant for the portrait
            SetHighlight(SlotHighlight.None);
        }
    }

    // Keeps the Image enabled even when empty - a disabled Graphic is skipped by GraphicRaycaster
    // entirely, which would make empty slots impossible to drop onto. Hiding it via alpha instead
    // keeps it a valid drop target.
    public void SetChampion(CharacterData champion)
    {
        Champion = champion;
        portraitImage.sprite = champion != null ? champion.portrait : null;
        portraitImage.color = new Color(1f, 1f, 1f, champion != null ? 1f : 0f);
    }

    // Dims the source slot's portrait while it's being dragged, so it visibly "leaves". Gets
    // restored automatically - RefreshAll (via SetChampion) runs at the end of every drag.
    public void SetDimmed(bool dimmed)
    {
        Color c = portraitImage.color;
        c.a = dimmed ? 0.35f : (Champion != null ? 1f : 0f);
        portraitImage.color = c;
    }

    public void SetHighlight(SlotHighlight state)
    {
        if (highlightImage == null) return;

        switch (state)
        {
            case SlotHighlight.None:
                highlightImage.enabled = false;
                break;
            case SlotHighlight.DropZone:
                highlightImage.enabled = true;
                highlightImage.color = new Color(1f, 1f, 1f, 0.25f);
                break;
            case SlotHighlight.Hovered:
                highlightImage.enabled = true;
                highlightImage.color = new Color(1f, 0.9f, 0.4f, 0.6f);
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasChampion) return;
        controller.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData) => controller.UpdateDrag(eventData);

    public void OnEndDrag(PointerEventData eventData) => controller.EndDrag(eventData);

    // A drop here is either another champion portrait being swapped in (handled by
    // PartyBenchUIController) or an item icon dragged out of the Item Bank (handled by whichever
    // ItemSlotUI/ItemBankUIController it came from) - tell them apart by what component the
    // originally-dragged object carries.
    public void OnDrop(PointerEventData eventData)
    {
        ItemSlotUI droppedItem = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponent<ItemSlotUI>() : null;
        if (droppedItem != null)
        {
            droppedItem.NotifyDroppedOnCharacter(Champion);
            return;
        }

        controller.DropOnSlot(this);
    }

    public void OnPointerEnter(PointerEventData eventData) => controller.NotifyHover(this, true);

    public void OnPointerExit(PointerEventData eventData) => controller.NotifyHover(this, false);

    public CharacterData GetInspectedChampion() => Champion;
}