using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Owns the two overlay panels (Bench and Party) and every drag-and-drop interaction between
// them. Both panels can be open at the same time on purpose - dragging a champion from one to
// the other is the main way the player manages their squad. Meant to live on the same
// screen/canvas as RouletteUIManager (the shop), so the tiny buttons are reachable while shopping.
public class PartyBenchUIController : MonoBehaviour
{
    [Header("Refs")]
    public PartyManager partyManager;
    public PlayerRoster roster;
    public PlayerParty party;

    [Header("Panels")]
    public GameObject benchPanel;
    public GameObject partyPanel;
    public Transform benchSlotsParent;
    public Transform partyFrontlineParent;
    public Transform partyBacklineParent;
    public ChampionSlotUI slotPrefab;

    [Header("Tiny Toggle Buttons")]
    public Button openBenchButton;
    public Button openPartyButton;

    [Header("Drag Ghost")]
    public Canvas rootCanvas;
    public Image dragGhostImage;

    readonly List<ChampionSlotUI> benchSlots = new List<ChampionSlotUI>();
    readonly List<ChampionSlotUI> partyFrontlineSlots = new List<ChampionSlotUI>();
    readonly List<ChampionSlotUI> partyBacklineSlots = new List<ChampionSlotUI>();

    ChampionSlotUI draggedSlot;
    bool dropWasHandled;

    void Awake()
    {
        BuildSlots();

        dragGhostImage.raycastTarget = false; // must not block the raycast to whatever's underneath it
        dragGhostImage.enabled = false;
    }

    void OnEnable()
    {
        openBenchButton.onClick.AddListener(ToggleBenchPanel);
        openPartyButton.onClick.AddListener(TogglePartyPanel);

        // Listen at the data source, not at whoever happens to trigger the change - this way
        // buying a champion on the Roulette (which never talks to PartyManager) still refreshes.
        roster.OnBenchChanged += RefreshAll;
        party.OnChanged += RefreshAll;
        partyManager.OnActionFailed += HandleActionFailed;

        RefreshAll();
    }

    void OnDisable()
    {
        openBenchButton.onClick.RemoveListener(ToggleBenchPanel);
        openPartyButton.onClick.RemoveListener(TogglePartyPanel);

        roster.OnBenchChanged -= RefreshAll;
        party.OnChanged -= RefreshAll;
        partyManager.OnActionFailed -= HandleActionFailed;
    }

    void BuildSlots()
    {
        for (int i = 0; i < roster.benchCapacity; i++)
            benchSlots.Add(CreateSlot(benchSlotsParent, ChampionSlotRef.BenchSlot(i)));

        for (int i = 0; i < PlayerParty.FrontlineCapacity; i++)
            partyFrontlineSlots.Add(CreateSlot(partyFrontlineParent, ChampionSlotRef.PartySlot(BattlePosition.Frontline, i)));

        for (int i = 0; i < PlayerParty.BacklineCapacity; i++)
            partyBacklineSlots.Add(CreateSlot(partyBacklineParent, ChampionSlotRef.PartySlot(BattlePosition.Backline, i)));
    }

    ChampionSlotUI CreateSlot(Transform parent, ChampionSlotRef slotRef)
    {
        ChampionSlotUI slot = Instantiate(slotPrefab, parent);
        slot.Initialize(this, slotRef);
        return slot;
    }

    void ToggleBenchPanel() => benchPanel.SetActive(!benchPanel.activeSelf);
    void TogglePartyPanel() => partyPanel.SetActive(!partyPanel.activeSelf);

    void HandleActionFailed(string reason) => Debug.Log($"Party action failed: {reason}");

    public void RefreshAll()
    {
        for (int i = 0; i < benchSlots.Count; i++) benchSlots[i].SetChampion(roster.GetBenchAt(i));
        for (int i = 0; i < partyFrontlineSlots.Count; i++) partyFrontlineSlots[i].SetChampion(party.GetChampionAt(BattlePosition.Frontline, i));
        for (int i = 0; i < partyBacklineSlots.Count; i++) partyBacklineSlots[i].SetChampion(party.GetChampionAt(BattlePosition.Backline, i));
    }

    // --- Drag coordination, called by ChampionSlotUI ---

    public void BeginDrag(ChampionSlotUI slot, PointerEventData eventData)
    {
        draggedSlot = slot;
        dropWasHandled = false;

        dragGhostImage.sprite = slot.portraitImage.sprite;
        dragGhostImage.color = Color.white; // override any stray alpha left on the ghost in the Editor
        dragGhostImage.enabled = true;
        dragGhostImage.transform.SetAsLastSibling();
        UpdateDrag(eventData);

        slot.SetDimmed(true);
        SetAllHighlights(SlotHighlight.DropZone);
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (draggedSlot == null) return;

        // Screen -> local-in-canvas conversion, so this tracks the cursor correctly regardless
        // of whether the Canvas is Overlay, Screen Space Camera, or World Space.
        RectTransform canvasRect = (RectTransform)rootCanvas.transform;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out Vector2 localPoint))
            ((RectTransform)dragGhostImage.transform).anchoredPosition = localPoint;
    }

    // Called on the slot the pointer is released over, before EndDrag fires on the dragged slot.
    public void DropOnSlot(ChampionSlotUI targetSlot)
    {
        if (draggedSlot == null || targetSlot == draggedSlot) return;

        dropWasHandled = true;
        partyManager.SwapSlots(draggedSlot.SlotRef, targetSlot.SlotRef);
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (draggedSlot == null) return;

        if (!dropWasHandled && !DroppedInsideAnyPanel(eventData))
            partyManager.SellChampion(draggedSlot.SlotRef);

        dragGhostImage.enabled = false;
        draggedSlot = null;
        SetAllHighlights(SlotHighlight.None);
        RefreshAll();
    }

    // Called by any slot's OnPointerEnter/OnPointerExit - only matters while a drag is live.
    public void NotifyHover(ChampionSlotUI slot, bool isHovering)
    {
        if (draggedSlot == null) return;
        slot.SetHighlight(isHovering ? SlotHighlight.Hovered : SlotHighlight.DropZone);
    }

    void SetAllHighlights(SlotHighlight state)
    {
        foreach (ChampionSlotUI slot in benchSlots) slot.SetHighlight(state);
        foreach (ChampionSlotUI slot in partyFrontlineSlots) slot.SetHighlight(state);
        foreach (ChampionSlotUI slot in partyBacklineSlots) slot.SetHighlight(state);
    }

    bool DroppedInsideAnyPanel(PointerEventData eventData)
    {
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        bool insideBench = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)benchPanel.transform, eventData.position, cam);
        bool insideParty = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)partyPanel.transform, eventData.position, cam);

        return insideBench || insideParty;
    }
}