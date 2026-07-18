using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One instance per scene, living on the panel's root (start disabled, direct child of the root
// Canvas, anchored at its center). Any CharacterInspectTrigger just calls Show() on it.
public class CharacterInfoPanelController : MonoBehaviour
{
    public static CharacterInfoPanelController Instance { get; private set; }

    [Header("Refs")]
    public PlayerRoster roster; // optional - if null, level always shows as 1 (e.g. inspecting an enemy/boss)
    public CharacterEquipmentManager equipmentManager; // optional - if null, the equipment row stays empty
    public PlayerParty party; // optional - if set, the ability matching the champion's party position gets an "active" marker
    public GameObject panelRoot;
    public Button closeButton;

    [Header("Identity")]
    public Image portraitImage;
    public TMP_Text nameLabel;
    public TMP_Text levelLabel;
    public TMP_Text mementoLabel; // optional: progress towards the next level-up, e.g. "2/3"

    [Header("Attributes")]
    public TMP_Text hpLabel;
    public TMP_Text adLabel;
    public TMP_Text apLabel;
    public TMP_Text atkSpeedLabel;
    public TMP_Text critLabel;
    public TMP_Text defLabel;
    public TMP_Text mdefLabel;
    public TMP_Text resourceLabel;

    [Header("Abilities")]
    public AbilityRowUI frontlineAbilityRow;
    public AbilityRowUI backlineAbilityRow;

    [Header("Equipment (size must match CharacterEquipmentManager.SlotsPerCharacter)")]
    public EquippedItemSlotUI[] equipmentSlots;

    CharacterData currentChampion;
    RectTransform panelRect;
    RectTransform canvasRect;
    Camera uiCamera;

    // Optional per-Show source for which ability row is active (e.g. a live BattleUnit's
    // abilityPosition). When null, falls back to the champion's PlayerParty position.
    Func<BattlePosition?> activeAbilityProvider;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        panelRect = (RectTransform)panelRoot.transform;
        Canvas canvas = panelRoot.GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = (RectTransform)canvas.transform;
        uiCamera = UIPositioning.CameraFor(canvas);

        panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (equipmentManager != null) equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        if (party != null) party.OnChanged += HandlePartyChanged;
    }

    void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        if (equipmentManager != null) equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        if (party != null) party.OnChanged -= HandlePartyChanged;
    }

    public void Show(CharacterData champion) => Show(champion, null);

    public void Show(CharacterData champion, Func<BattlePosition?> activeProvider)
    {
        if (champion == null) return;

        currentChampion = champion;
        activeAbilityProvider = activeProvider;
        PopulateContent(champion);

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        currentChampion = null;
        activeAbilityProvider = null;
        panelRoot.SetActive(false);
        AbilityTooltip.Instance?.Hide();
    }

    // The active kit can change while the panel is open (e.g. Nikkal repositioning
    // mid-battle), so provider-driven markers are kept live every frame
    void Update()
    {
        if (currentChampion != null && activeAbilityProvider != null && panelRoot.activeSelf)
            ApplyActiveMarkers(activeAbilityProvider());
    }

    // Keeps the panel live while it's open - e.g. clicking an equipped item's icon to unequip it
    // should update the icons and stats immediately, without closing and reopening the panel.
    void HandleEquipmentChanged(CharacterData champion)
    {
        if (currentChampion != null && currentChampion == champion) PopulateContent(champion);
    }

    void HandlePartyChanged()
    {
        if (currentChampion != null) PopulateContent(currentChampion);
    }

    void PopulateContent(CharacterData champion)
    {
        int level = roster != null ? Mathf.Max(roster.GetLevel(champion), 1) : 1;
        CharacterStats stats = equipmentManager != null
            ? equipmentManager.GetEffectiveStats(champion)
            : LevelUpCalculator.GetStatsAtLevel(champion, level);

        portraitImage.sprite = champion.portrait;
        nameLabel.text = champion.characterName;
        levelLabel.text = $"Lv. {level}";

        if (mementoLabel != null)
            mementoLabel.text = roster != null ? $"{roster.GetMementoCount(champion)}/{roster.mementosToLevelUp}" : "";

        hpLabel.text = $"HP: {stats.maxHP:0}";
        adLabel.text = $"AD: {stats.AD:0}";
        apLabel.text = $"AP: {stats.AP:0}";
        atkSpeedLabel.text = $"ATK SPD: {stats.attackSpeed:0.00}";
        critLabel.text = $"CRIT: {stats.critChance * 100f:0}%";
        defLabel.text = $"DEF: {stats.DEF:0}";
        mdefLabel.text = $"MDEF: {stats.MDEF:0}";
        resourceLabel.text = stats.manaPerSecond > 0f ? $"Mana/s: {stats.manaPerSecond:0.0}" : "Resource: none";

        frontlineAbilityRow.Bind(champion.frontlineAbility, "F");
        backlineAbilityRow.Bind(champion.backlineAbility, "B");
        ApplyActiveMarkers(ResolveActivePosition(champion));

        if (equipmentManager != null && equipmentSlots != null)
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
                equipmentSlots[i].Bind(equipmentManager, champion, i);
        }
    }

    BattlePosition? ResolveActivePosition(CharacterData champion)
    {
        if (activeAbilityProvider != null) return activeAbilityProvider();
        if (party != null && party.FindSlot(champion, out BattlePosition position, out _)) return position;
        return null;
    }

    void ApplyActiveMarkers(BattlePosition? active)
    {
        frontlineAbilityRow.SetActiveMarker(active == BattlePosition.Frontline);
        backlineAbilityRow.SetActiveMarker(active == BattlePosition.Backline);
    }
}