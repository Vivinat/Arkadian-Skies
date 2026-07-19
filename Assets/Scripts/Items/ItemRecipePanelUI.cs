using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One instance per scene. Left-clicking a bank item calls Show(): a component lists
// every complete item it can forge, a complete item lists its own recipe.
public class ItemRecipePanelUI : MonoBehaviour
{
    public static ItemRecipePanelUI Instance { get; private set; }

    public ItemRecipeBook recipeBook;
    public GameObject panelRoot;
    public TMP_Text titleLabel;
    public TMP_Text emptyLabel;
    public Button closeButton;
    public ItemRecipeRowUI[] rows;

    void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }

    public void Show(ItemComponentData item)
    {
        if (item == null) return;

        titleLabel.text = $"{item.itemName} - Recipes";

        List<(ItemComponentData a, ItemComponentData b, ItemComponentData result)> entries = Collect(item);
        for (int i = 0; i < rows.Length; i++)
        {
            if (i < entries.Count) rows[i].Bind(entries[i].a, entries[i].b, entries[i].result);
            else rows[i].Clear();
        }

        if (emptyLabel != null) emptyLabel.gameObject.SetActive(entries.Count == 0);

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
        AbilityTooltip.Instance?.Hide();
    }

    List<(ItemComponentData, ItemComponentData, ItemComponentData)> Collect(ItemComponentData item)
    {
        var entries = new List<(ItemComponentData, ItemComponentData, ItemComponentData)>();
        if (recipeBook == null) return entries;

        if (item.isCompleteItem)
        {
            entries.Add((item.recipeComponentA, item.recipeComponentB, item));
            return entries;
        }

        foreach (ItemComponentData complete in recipeBook.completeItems)
        {
            if (complete == null) continue;
            if (complete.recipeComponentA == item || complete.recipeComponentB == item)
                entries.Add((complete.recipeComponentA, complete.recipeComponentB, complete));
        }
        return entries;
    }
}
