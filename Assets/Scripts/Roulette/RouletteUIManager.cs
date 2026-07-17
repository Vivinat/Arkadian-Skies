using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouletteUIManager : MonoBehaviour
{
    public RouletteManager roulette;
    public PlayerGold playerGold;

    [Header("Roullete Panel")]
    public Transform slotsParent; // the "Roullete" panel - slots are instantiated as its children
    public GameObject slotButtonPrefab; // Button prefab whose own Image is the portrait

    [Header("Gold")]
    public TMP_Text goldLabel;

    [Header("Rarity Selection + Spin")]
    public Button setCommonButton;
    public Button setEpicButton;
    public Button setLegendaryButton;
    public Button spinButton;
    public TMP_Text spinCostLabel;

    ChampionRarity selectedRarity = ChampionRarity.Common;
    readonly List<RouletteSlotButton> slots = new List<RouletteSlotButton>();

    void Awake()
    {
        BuildSlots();
    }

    // Clears whatever placeholder children are sitting in the panel and instantiates the real slots.
    void BuildSlots()
    {
        foreach (Transform child in slotsParent) Destroy(child.gameObject);
        slots.Clear();

        for (int i = 0; i < roulette.resultSlotCount; i++)
        {
            GameObject instance = Instantiate(slotButtonPrefab, slotsParent);
            RouletteSlotButton slot = instance.GetComponent<RouletteSlotButton>();

            int index = i; // capture for the closure
            slot.button.onClick.AddListener(() => roulette.Acquire(index));
            slot.SetChampion(null);

            slots.Add(slot);
        }
    }

    void OnEnable()
    {
        roulette.OnRouletteSpun += HandleSpun;
        roulette.OnSlotAcquired += HandleAcquired;
        playerGold.OnGoldChanged += RefreshGold;

        setCommonButton.onClick.AddListener(() => SelectRarity(ChampionRarity.Common));
        setEpicButton.onClick.AddListener(() => SelectRarity(ChampionRarity.Epic));
        setLegendaryButton.onClick.AddListener(() => SelectRarity(ChampionRarity.Legendary));
        spinButton.onClick.AddListener(() => roulette.Spin(selectedRarity));

        RefreshGold(playerGold.Current);
        SelectRarity(selectedRarity);
        HandleSpun(roulette.CurrentResults); // restore last spin's results if the panel was reopened
    }

    void OnDisable()
    {
        roulette.OnRouletteSpun -= HandleSpun;
        roulette.OnSlotAcquired -= HandleAcquired;
        playerGold.OnGoldChanged -= RefreshGold;

        setCommonButton.onClick.RemoveAllListeners();
        setEpicButton.onClick.RemoveAllListeners();
        setLegendaryButton.onClick.RemoveAllListeners();
        spinButton.onClick.RemoveAllListeners();
    }

    void SelectRarity(ChampionRarity rarity)
    {
        selectedRarity = rarity;

        // cheap "selected" feedback: the button matching the current choice gets disabled
        setCommonButton.interactable = rarity != ChampionRarity.Common;
        setEpicButton.interactable = rarity != ChampionRarity.Epic;
        setLegendaryButton.interactable = rarity != ChampionRarity.Legendary;

        spinCostLabel.text = $"Spin ({RouletteCosts.SpinCost(rarity)}g)";
    }

    void HandleSpun(List<RouletteResult> results)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < results.Count)
            {
                slots[i].SetChampion(results[i].champion);
                slots[i].SetClaimed(results[i].claimed);
            }
            else
            {
                slots[i].SetChampion(null);
            }
        }

        RefreshBadges();
    }

    void HandleAcquired(int slotIndex, RouletteResult result)
    {
        if (slotIndex < slots.Count) slots[slotIndex].SetClaimed(result.claimed);
        RefreshBadges();
    }

    // Copies of an owned champion count as mementos - each slot's badge shows how close
    // that champion is to the next level up
    void RefreshBadges()
    {
        PlayerRoster roster = roulette.playerRoster;
        if (roster == null) return;

        foreach (RouletteSlotButton slot in slots)
        {
            CharacterData champion = slot.Champion;
            if (champion == null)
            {
                slot.SetOwnership(false, 0, 0, 0);
                continue;
            }

            slot.SetOwnership(roster.Owns(champion), roster.GetLevel(champion),
                roster.GetMementoCount(champion), roster.mementosToLevelUp);
        }
    }

    void RefreshGold(int amount) => goldLabel.text = $"{amount}g";
}