using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Owns the Item Bank overlay panel and the drag side of equipping: dragging an item icon out of
// here and releasing it over a champion portrait (ChampionSlotUI, bench or party, doesn't matter
// which) equips it. Meant to live alongside PartyBenchUIController on the same screen/canvas.
public class ItemBankUIController : MonoBehaviour
{
    [Header("Refs")]
    public PlayerItemBank bank;
    public CharacterEquipmentManager equipmentManager;

    [Header("Panel")]
    public GameObject bankPanel;
    public Transform bankSlotsParent;
    public ItemSlotUI slotPrefab;

    [Header("Tiny Toggle Button")]
    public Button openBankButton;

    [Header("Drag Ghost")]
    public Canvas rootCanvas;
    public Image dragGhostImage;

    readonly List<ItemSlotUI> slots = new List<ItemSlotUI>();
    static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    ItemSlotUI draggedSlot;
    ChampionSlotUI hoveredChampionSlot;

    void Awake()
    {
        for (int i = 0; i < bank.capacity; i++)
        {
            ItemSlotUI slot = Instantiate(slotPrefab, bankSlotsParent);
            slot.Initialize(this);
            slots.Add(slot);
        }

        dragGhostImage.raycastTarget = false; // must not block the raycast to whatever's underneath it
        dragGhostImage.enabled = false;
    }

    void OnEnable()
    {
        openBankButton.onClick.AddListener(TogglePanel);
        bank.OnBankChanged += RefreshAll;
        equipmentManager.OnActionFailed += HandleActionFailed;

        RefreshAll();
    }

    void OnDisable()
    {
        openBankButton.onClick.RemoveListener(TogglePanel);
        bank.OnBankChanged -= RefreshAll;
        equipmentManager.OnActionFailed -= HandleActionFailed;
    }

    void TogglePanel() => bankPanel.SetActive(!bankPanel.activeSelf);

    void HandleActionFailed(string reason) => Debug.Log($"Item action failed: {reason}");

    void RefreshAll()
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].SetItem(bank.GetAt(i), i);
    }

    // --- Drag coordination, called by ItemSlotUI ---

    public void BeginDrag(ItemSlotUI slot, PointerEventData eventData)
    {
        draggedSlot = slot;

        dragGhostImage.sprite = slot.iconImage.sprite;
        dragGhostImage.color = Color.white;
        dragGhostImage.enabled = true;
        dragGhostImage.transform.SetAsLastSibling();
        UpdateDrag(eventData);

        slot.SetDimmed(true);
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (draggedSlot == null) return;

        RectTransform canvasRect = (RectTransform)rootCanvas.transform;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out Vector2 localPoint))
            ((RectTransform)dragGhostImage.transform).anchoredPosition = localPoint;

        UpdateHoveredChampionSlot(eventData);
    }

    // Highlights whichever champion portrait the item is currently over, reusing the same
    // SlotHighlight enum ChampionSlotUI already exposes for champion-to-champion drags.
    void UpdateHoveredChampionSlot(PointerEventData eventData)
    {
        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        ChampionSlotUI hit = null;
        foreach (RaycastResult result in raycastResults)
        {
            hit = result.gameObject.GetComponentInParent<ChampionSlotUI>();
            if (hit != null) break;
        }

        if (hit == hoveredChampionSlot) return;

        if (hoveredChampionSlot != null) hoveredChampionSlot.SetHighlight(SlotHighlight.None);
        if (hit != null) hit.SetHighlight(SlotHighlight.Hovered);
        hoveredChampionSlot = hit;
    }

    // Called by ItemSlotUI.NotifyDroppedOnCharacter, forwarded from whichever ChampionSlotUI the
    // icon was released over.
    public void RequestEquip(ItemSlotUI sourceSlot, CharacterData champion)
    {
        if (champion == null)
        {
            Debug.Log("Item action failed: no champion in that slot to equip onto.");
            return;
        }

        equipmentManager.EquipToFirstEmptySlot(champion, sourceSlot.BankIndex);
    }

    // Called by ItemSlotUI.OnDrop when one bank item is released over another slot
    public void RequestMove(ItemSlotUI source, ItemSlotUI target)
    {
        bank.Move(source.BankIndex, target.BankIndex);
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (draggedSlot == null) return;

        if (hoveredChampionSlot != null)
        {
            hoveredChampionSlot.SetHighlight(SlotHighlight.None);
            hoveredChampionSlot = null;
        }

        dragGhostImage.enabled = false;
        draggedSlot = null;
        RefreshAll();
    }
}