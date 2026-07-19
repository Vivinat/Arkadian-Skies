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

    [Tooltip("Seconds a dragged component must hover over its recipe partner to forge")]
    public float combineHoldSeconds = 3f;

    readonly List<ItemSlotUI> slots = new List<ItemSlotUI>();
    static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    ItemSlotUI draggedSlot;
    ChampionSlotUI hoveredChampionSlot;
    ItemSlotUI combineTarget;
    float combineTimer;

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
        UpdateCombineTarget();
    }

    // Hold-to-forge: while the dragged component hovers a slot whose item completes a
    // recipe with it, a progress bar fills under that slot; leaving the slot cancels it
    void UpdateCombineTarget()
    {
        ItemSlotUI hit = null;
        foreach (RaycastResult result in raycastResults)
        {
            ItemSlotUI slot = result.gameObject.GetComponentInParent<ItemSlotUI>();
            if (slot != null && slot != draggedSlot)
            {
                hit = slot;
                break;
            }
        }

        bool valid = hit != null && hit.HasItem && draggedSlot != null && draggedSlot.HasItem
            && bank.recipeBook != null
            && bank.recipeBook.FindRecipe(draggedSlot.Item, hit.Item) != null;

        ItemSlotUI newTarget = valid ? hit : null;
        if (newTarget == combineTarget) return;

        ClearCombineTarget();
        combineTarget = newTarget;
        combineTimer = 0f;
    }

    void ClearCombineTarget()
    {
        if (combineTarget != null) combineTarget.HideCombineProgress();
        combineTarget = null;
        combineTimer = 0f;
    }

    // OnDrag only fires while the pointer moves, so the hold timer advances here instead.
    // Unscaled time keeps the 5s hold consistent across battle speeds.
    void Update()
    {
        if (draggedSlot == null || combineTarget == null) return;

        bool stillValid = draggedSlot.HasItem && combineTarget.Item != null
            && bank.recipeBook != null
            && bank.recipeBook.FindRecipe(draggedSlot.Item, combineTarget.Item) != null;
        if (!stillValid)
        {
            ClearCombineTarget();
            return;
        }

        combineTimer += Time.unscaledDeltaTime;
        combineTarget.SetCombineProgress(combineTimer / combineHoldSeconds);

        if (combineTimer >= combineHoldSeconds) CompleteCombine();
    }

    void CompleteCombine()
    {
        ItemSlotUI target = combineTarget;
        int fromIndex = draggedSlot.BankIndex;
        ClearCombineTarget();

        // the dragged item is consumed, so the drag visuals end right here - the later
        // OnEndDrag from the release is a no-op thanks to the draggedSlot null guard
        if (hoveredChampionSlot != null)
        {
            hoveredChampionSlot.SetHighlight(SlotHighlight.None);
            hoveredChampionSlot = null;
        }
        dragGhostImage.enabled = false;
        draggedSlot = null;

        bank.TryCombine(fromIndex, target.BankIndex);
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

    // Called by ItemSlotUI.OnDrop when one bank item is released over another slot.
    // Dropping only reorders - forging happens by HOLDING over the recipe partner.
    public void RequestMove(ItemSlotUI source, ItemSlotUI target)
    {
        bank.Move(source.BankIndex, target.BankIndex);
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (draggedSlot == null) return;

        ClearCombineTarget();

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