using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Owns the OTHER ITEMS panel and every hold-to-use interaction. Dragging a consumable
// over a valid target (a complete item for Disassembler/Delirium, a champion for
// Nostalgia) fills a progress bar on that target; releasing or leaving cancels.
public class OtherItemBankUI : MonoBehaviour
{
    public PlayerOtherItemBank bank;
    public OtherItemManager manager;
    public ArtisanAspectPanelUI artisanPanel;
    public OtherItemSlotUI[] slots;

    public Canvas rootCanvas;
    public Image dragGhostImage;
    public float holdSeconds = 2.5f;

    static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    OtherItemSlotUI draggedSlot;
    ItemSlotUI itemTarget;
    ChampionSlotUI championTarget;
    float holdTimer;

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].Initialize(this, i);

        dragGhostImage.raycastTarget = false;
        dragGhostImage.enabled = false;
    }

    void OnEnable()
    {
        bank.OnChanged += RefreshAll;
        RefreshAll();
    }

    void OnDisable()
    {
        bank.OnChanged -= RefreshAll;
    }

    void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetStack(bank.GetItemAt(i), bank.GetCountAt(i));
    }

    // --- drag coordination ---

    public void BeginDrag(OtherItemSlotUI slot, PointerEventData eventData)
    {
        draggedSlot = slot;
        dragGhostImage.sprite = slot.iconImage.sprite;
        dragGhostImage.color = Color.white;
        dragGhostImage.enabled = slot.iconImage.sprite != null;
        dragGhostImage.transform.SetAsLastSibling();
        UpdateDrag(eventData);
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (draggedSlot == null) return;

        RectTransform canvasRect = (RectTransform)rootCanvas.transform;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out Vector2 localPoint))
            ((RectTransform)dragGhostImage.transform).anchoredPosition = localPoint;

        UpdateTarget(eventData);
    }

    void UpdateTarget(PointerEventData eventData)
    {
        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        ItemSlotUI newItemTarget = null;
        ChampionSlotUI newChampionTarget = null;

        OtherItemEffectType effect = draggedSlot.Item.effectType;
        foreach (RaycastResult result in raycastResults)
        {
            if (effect == OtherItemEffectType.Nostalgia)
            {
                ChampionSlotUI champ = result.gameObject.GetComponentInParent<ChampionSlotUI>();
                if (champ != null && champ.HasChampion) { newChampionTarget = champ; break; }
            }
            else
            {
                ItemSlotUI item = result.gameObject.GetComponentInParent<ItemSlotUI>();
                if (item != null && item.HasItem && item.Item.isCompleteItem) { newItemTarget = item; break; }
            }
        }

        if (newItemTarget != itemTarget || newChampionTarget != championTarget)
        {
            ClearTarget();
            itemTarget = newItemTarget;
            championTarget = newChampionTarget;
            holdTimer = 0f;
        }
    }

    void ClearTarget()
    {
        if (itemTarget != null) itemTarget.HideCombineProgress();
        if (championTarget != null) championTarget.HideHoldProgress();
        itemTarget = null;
        championTarget = null;
        holdTimer = 0f;
    }

    // OnDrag only fires while the pointer moves, so the hold timer advances here
    void Update()
    {
        if (draggedSlot == null || (itemTarget == null && championTarget == null)) return;

        holdTimer += Time.unscaledDeltaTime;
        float progress = holdTimer / holdSeconds;

        if (itemTarget != null) itemTarget.SetCombineProgress(progress);
        if (championTarget != null) championTarget.SetHoldProgress(progress);

        if (holdTimer >= holdSeconds) CompleteHold();
    }

    void CompleteHold()
    {
        OtherItemSlotUI used = draggedSlot;
        ItemSlotUI item = itemTarget;
        ChampionSlotUI champ = championTarget;

        ClearTarget();
        dragGhostImage.enabled = false;
        draggedSlot = null;

        bool success = false;
        switch (used.Item.effectType)
        {
            case OtherItemEffectType.Disassembler: success = manager.Disassemble(item.BankIndex); break;
            case OtherItemEffectType.Delirium: success = manager.Delirium(item.BankIndex); break;
            case OtherItemEffectType.Nostalgia: success = manager.Nostalgia(champ.SlotRef); break;
        }

        if (success) bank.ConsumeAt(used.Index);
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (draggedSlot == null) return;
        ClearTarget();
        dragGhostImage.enabled = false;
        draggedSlot = null;
    }

    // --- Artisan's Aspect (click) ---

    public void UseArtisan(OtherItemSlotUI slot)
    {
        if (artisanPanel == null) return;

        int slotIndex = slot.Index;
        artisanPanel.Open(manager.RollArtisanChoices(5), chosen =>
        {
            if (chosen != null && manager.GrantItem(chosen)) bank.ConsumeAt(slotIndex);
        });
    }
}
